using System.Text.Json;
using Crashguard.Engine.Data;
using Crashguard.Engine.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;

namespace Crashguard.Engine.Connectors;

/// <summary>
/// Sends a plain-text alert email via the instance-wide SMTP relay configured in Settings. Expects a
/// channel config shaped like <c>{ "addresses": ["ops@example.com", "oncall@example.com"] }</c>,
/// matching the frontend's Email connector — the SMTP server itself is configured once for the whole
/// instance rather than per channel, since channels only need to name recipients.
/// </summary>
public class EmailConnector(IServiceScopeFactory scopeFactory) : IConnector
{
    public string Type => "email";

    public async Task SendAsync(JsonElement config, Canary canary, CanaryType canaryType, string? adminPortalUrl, AlertContext alertContext, CancellationToken ct)
    {
        if (!config.TryGetProperty("addresses", out var addressesProp) || addressesProp.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Email connector config is missing an 'addresses' array.");
        }

        var addresses = addressesProp.EnumerateArray()
            .Select(a => a.GetString())
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a!)
            .ToList();

        if (addresses.Count == 0)
        {
            throw new InvalidOperationException("Email connector config's 'addresses' array is empty.");
        }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var settings = await db.Settings.SingleAsync(ct);

        if (string.IsNullOrWhiteSpace(settings.SmtpHost) || settings.SmtpPort is null || string.IsNullOrWhiteSpace(settings.SmtpFromAddress))
        {
            throw new InvalidOperationException("SMTP is not configured. Set the SMTP host, port, and from address in Settings.");
        }

        var verb = alertContext.IsRenotify ? "Still Firing" : "Triggered";
        var subject = $"[{alertContext.Severity}] Canary {verb}: {canaryType.Name} ({alertContext.DeduplicatedCount} instance{(alertContext.DeduplicatedCount == 1 ? "" : "s")})";

        var lines = new List<string>
        {
            $"Reference: {canary.ReferenceId}",
            $"Started: {canary.StartedAt:u}",
            $"Due: {canary.ExpiresAt:u}",
        };

        if (!string.IsNullOrWhiteSpace(adminPortalUrl))
        {
            var link = $"{adminPortalUrl}/canaries/{Uri.EscapeDataString(canary.CanaryType)}/{Uri.EscapeDataString(canary.ReferenceId)}";
            lines.Add($"View this canary: {link}");
        }
        if (!string.IsNullOrWhiteSpace(alertContext.DashboardUrl))
        {
            lines.Add($"View all triggered canaries in this window: {alertContext.DashboardUrl}");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.SmtpFromName ?? "CrashGuard", settings.SmtpFromAddress));
        foreach (var address in addresses)
        {
            message.To.Add(MailboxAddress.Parse(address));
        }
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = string.Join("\n", lines) };

        using var client = new SmtpClient();
        var secureSocketOptions = settings.SmtpUseTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort.Value, secureSocketOptions, ct);

        if (!string.IsNullOrWhiteSpace(settings.SmtpUsername))
        {
            await client.AuthenticateAsync(settings.SmtpUsername, settings.SmtpPassword ?? string.Empty, ct);
        }

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
