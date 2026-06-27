using System.Text.Json;
using Crashguard.Engine.Models;
using RestSharp;

namespace Crashguard.Engine.Connectors;

/// <summary>
/// Posts alerts to a Slack channel via an incoming webhook URL. Expects a channel config shaped like
/// <c>{ "webhookUrl": "https://hooks.slack.com/services/..." }</c>, matching the frontend's Slack connector.
/// Uses the webhook's <c>text</c> field with mrkdwn formatting — this assumes a classic Slack App
/// incoming webhook, not a Workflow Builder webhook (which ignores mrkdwn/blocks and expects a flat
/// set of variables instead).
/// </summary>
public class SlackConnector(RestClient restClient) : IConnector
{
    public string Type => "slack";

    public async Task SendAsync(JsonElement config, Canary canary, CanaryType canaryType, string? adminPortalUrl, AlertContext alertContext, CancellationToken ct)
    {
        if (!config.TryGetProperty("webhookUrl", out var webhookUrlProp) || webhookUrlProp.GetString() is not { Length: > 0 } webhookUrl)
        {
            throw new InvalidOperationException("Slack connector config is missing a 'webhookUrl'.");
        }

        var emoji = SeverityEmoji(alertContext.Severity);
        var verb = alertContext.IsRenotify ? "Still Firing" : "Triggered";
        var title = $"{emoji} *Canary {verb}: {canaryType.Name} [{alertContext.DeduplicatedCount} instance{(alertContext.DeduplicatedCount == 1 ? "" : "s")}]* {emoji}";

        var lines = new List<string>
        {
            title,
            "",
            $"• *Reference:* {canary.ReferenceId}",
            $"• *Started:* {canary.StartedAt:u}",
            $"• *Due:* {canary.ExpiresAt:u}",
        };

        if (!string.IsNullOrWhiteSpace(adminPortalUrl))
        {
            var link = $"{adminPortalUrl}/canaries/{Uri.EscapeDataString(canary.CanaryType)}/{Uri.EscapeDataString(canary.ReferenceId)}";
            lines.Add($"• <{link}|View this canary in the admin portal>");
        }
        if (!string.IsNullOrWhiteSpace(alertContext.DashboardUrl))
        {
            lines.Add($"• <{alertContext.DashboardUrl}|View all triggered canaries in this window>");
        }

        var payload = new { text = string.Join("\n", lines) };

        var request = new RestRequest(webhookUrl).AddJsonBody(payload);
        var response = await restClient.ExecutePostAsync(request, ct);
        response.ThrowIfError();
    }

    private static string SeverityEmoji(string severity) => severity switch
    {
        "critical" => "🚨",
        "warning" => "⚠️",
        "info" => "ℹ️",
        _ => "🔔",
    };
}
