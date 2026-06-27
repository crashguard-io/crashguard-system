using System.Text.Json;
using Crashguard.Engine.Models;

namespace Crashguard.Engine.Connectors;

/// <summary>
/// Dedup context for a triggered canary, describing the alert batch it belongs to. Connectors use
/// this to render a "first failure" alert differently from a "still firing" renotify summary.
/// </summary>
/// <param name="DeduplicatedCount">
/// Triggers folded into this batch since the last notification was sent (including this one).
/// Always 1 for a fresh batch's first alert.
/// </param>
/// <param name="WindowStart">When the earliest trigger folded into this batch actually happened.</param>
/// <param name="WindowEnd">When the most recent trigger folded into this batch actually happened.</param>
/// <param name="IsRenotify">True if an alert for this batch has already been sent and this is a periodic "still firing" update.</param>
/// <param name="DashboardUrl">Link to the admin portal filtered to this batch's window, if an admin portal URL is configured.</param>
/// <param name="Severity">
/// The effective severity for this batch — a matched rule's override if one applied, otherwise the
/// canary type's default. Connectors should use this (not the canary type's own severity) when
/// choosing how to present the alert, since a rule can downgrade or upgrade it per canary.
/// </param>
public record AlertContext(int DeduplicatedCount, DateTime WindowStart, DateTime WindowEnd, bool IsRenotify, string? DashboardUrl, string Severity);

public interface IConnector
{
    /// <summary>
    /// The channel "type" this connector handles, e.g. "slack". Matches <see cref="Models.Channel.Type"/>
    /// and the connector identifiers used by the frontend's connector registry.
    /// </summary>
    string Type { get; }

    /// <param name="adminPortalUrl">
    /// The externally-reachable admin portal URL, if the admin has configured one in Settings.
    /// Connectors should link to the canary's detail page when set, and omit the link otherwise.
    /// </param>
    /// <param name="alertContext">
    /// Dedup batch info for this canary's most recent trigger. Connectors should mention the
    /// deduplicated count and dashboard link when <see cref="AlertContext.DeduplicatedCount"/> is
    /// greater than 1, or whenever it's a renotify update.
    /// </param>
    Task SendAsync(JsonElement config, Canary canary, CanaryType canaryType, string? adminPortalUrl, AlertContext alertContext, CancellationToken ct);
}
