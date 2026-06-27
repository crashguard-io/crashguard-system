using System.Text.Json;
using Crashguard.Engine.Models;
using RestSharp;

namespace Crashguard.Engine.Connectors;

/// <summary>
/// Posts a JSON body describing the canary to an arbitrary URL. Expects a channel config shaped like
/// <c>{ "url": "https://example.com/hook" }</c>, matching the frontend's Webhook connector.
/// </summary>
public class WebhookConnector(RestClient restClient) : IConnector
{
    public string Type => "webhook";

    public async Task SendAsync(JsonElement config, Canary canary, CanaryType canaryType, string? adminPortalUrl, AlertContext alertContext, CancellationToken ct)
    {
        if (!config.TryGetProperty("url", out var urlProp) || urlProp.GetString() is not { Length: > 0 } url)
        {
            throw new InvalidOperationException("Webhook connector config is missing a 'url'.");
        }

        var payload = new
        {
            canaryType = canaryType.Name,
            referenceId = canary.ReferenceId,
            status = canary.Status,
            startedAt = canary.StartedAt,
            expiresAt = canary.ExpiresAt,
            triggeredAt = canary.TriggeredAt,
            resolvedAt = canary.ResolvedAt,
            metadata = canary.Metadata is not null ? JsonSerializer.Deserialize<JsonElement>(canary.Metadata) : (JsonElement?)null,
            severity = alertContext.Severity,
            isRenotify = alertContext.IsRenotify,
            deduplicatedCount = alertContext.DeduplicatedCount,
            windowStart = alertContext.WindowStart,
            windowEnd = alertContext.WindowEnd,
            dashboardUrl = alertContext.DashboardUrl,
            adminPortalUrl = string.IsNullOrWhiteSpace(adminPortalUrl)
                ? null
                : $"{adminPortalUrl}/canaries/{Uri.EscapeDataString(canary.CanaryType)}/{Uri.EscapeDataString(canary.ReferenceId)}",
        };

        var request = new RestRequest(url).AddJsonBody(payload);
        var response = await restClient.ExecutePostAsync(request, ct);
        response.ThrowIfError();
    }
}
