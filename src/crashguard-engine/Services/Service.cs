using System.Text.Json;
using Crashguard.Engine.Connectors;
using Crashguard.Engine.Data;
using Crashguard.Engine.Models;
using Crashguard.Engine.Verifiers;
using Microsoft.EntityFrameworkCore;

namespace Crashguard.Engine.Services;

public class Service(IServiceScopeFactory scopeFactory, ILogger<Service> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
    private const int MaxConcurrentVerifierCalls = 25;

    /// <summary>
    /// Sentinel destination channel used when no rule matched a canary, meaning it routes through
    /// the canary type's configured default channels at the type's default severity.
    /// </summary>
    public const string DefaultDestinationChannel = "__default__";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Crashguard engine started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckOverdueCanariesAsync(stoppingToken);
                await SweepAlertBatchesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error while checking overdue canaries.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }

        logger.LogInformation("Crashguard engine stopped.");
    }

    private async Task CheckOverdueCanariesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var verifierClient = scope.ServiceProvider.GetRequiredService<IVerifierClient>();
        var connectorRegistry = scope.ServiceProvider.GetRequiredService<ConnectorRegistry>();

        var now = DateTime.UtcNow;
        var overdueCanaries = await db.Canaries
            .Where(c => c.Status == "Pending" && c.ExpiresAt <= now)
            .ToListAsync(ct);

        if (overdueCanaries.Count == 0) return;

        var canaryTypes = await db.CanaryTypes
            .Include(t => t.DefaultChannels)
            .Include(t => t.Rules)
            .AsSplitQuery()
            .ToDictionaryAsync(t => t.Name, ct);

        // Triggers accumulated across this whole tick, so alerting can be decided once all of this
        // tick's failures are known rather than one at a time — otherwise a single burst of triggers
        // would only ever report "1 triggered" to the first one processed, with the rest silently
        // folded into the batch's counters with nothing in the alert message to show for it.
        var triggeredThisTick = new List<(Canary Canary, CanaryType? CanaryType)>();

        // Partition: canaries with no verifier to call (missing type, no VerifierUrl, or extend budget
        // exhausted) trigger immediately. Canaries that need a verifier go through a separate pass so
        // the (potentially slow) HTTP calls can be fanned out concurrently below.
        var toVerify = new List<(Canary Canary, CanaryType CanaryType)>();

        foreach (var canary in overdueCanaries)
        {
            if (!canaryTypes.TryGetValue(canary.CanaryType, out var canaryType))
            {
                logger.LogWarning(
                    "Canary {CanaryType}/{ReferenceId} is overdue but no matching canary type is configured; triggering.",
                    canary.CanaryType, canary.ReferenceId);
                await MarkTriggeredAsync(canary, db, ct);
                triggeredThisTick.Add((canary, null));
                continue;
            }

            var hasVerifier = !string.IsNullOrWhiteSpace(canaryType.VerifierUrl);
            var extendLimitReached = canary.ExtendCount >= canaryType.ExtendLimit;

            if (!hasVerifier || extendLimitReached)
            {
                await MarkTriggeredAsync(canary, db, ct);
                triggeredThisTick.Add((canary, canaryType));
                continue;
            }

            toVerify.Add((canary, canaryType));
        }

        if (toVerify.Count > 0)
        {
            // Network-only phase: no DbContext is touched inside the parallel branches, since
            // DbContext isn't thread-safe and SQLite serializes writes anyway. Bounding concurrency
            // avoids hammering verifier endpoints (and our own outbound connection pool) at scale.
            var verifierResults = new (Canary Canary, CanaryType CanaryType, VerifierResponse? Response)[toVerify.Count];

            await Parallel.ForEachAsync(
                Enumerable.Range(0, toVerify.Count),
                new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrentVerifierCalls, CancellationToken = ct },
                async (i, innerCt) =>
                {
                    var (canary, canaryType) = toVerify[i];
                    try
                    {
                        var response = await verifierClient.VerifyAsync(canaryType.VerifierUrl!, canary, innerCt);
                        verifierResults[i] = (canary, canaryType, response);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        logger.LogError(
                            ex, "Verifier call failed for canary {CanaryType}/{ReferenceId}; triggering.",
                            canary.CanaryType, canary.ReferenceId);
                        verifierResults[i] = (canary, canaryType, null);
                    }
                });

            // Apply phase: sequential against the single scoped DbContext.
            foreach (var (canary, canaryType, response) in verifierResults)
            {
                if (response is null)
                {
                    await MarkTriggeredAsync(canary, db, ct);
                    triggeredThisTick.Add((canary, canaryType));
                    continue;
                }

                switch (response.Action)
                {
                    case VerifierAction.Extend:
                        canary.ExtendCount++;
                        canary.ExpiresAt = now.AddSeconds(canary.Timeout);
                        await db.SaveChangesAsync(ct);
                        break;

                    case VerifierAction.Resolve:
                        canary.Status = "Resolved";
                        canary.ResolvedAt = now;
                        await db.SaveChangesAsync(ct);
                        break;

                    case VerifierAction.Trigger:
                    default:
                        await MarkTriggeredAsync(canary, db, ct);
                        triggeredThisTick.Add((canary, canaryType));
                        break;
                }
            }
        }

        foreach (var group in triggeredThisTick.Where(t => t.CanaryType is not null).GroupBy(t => t.CanaryType!.Id))
        {
            var canaryType = group.First().CanaryType!;
            var canaries = group.Select(g => g.Canary).ToList();
            await ProcessAlertGroupAsync(canaryType, canaries, connectorRegistry, db, ct);
        }
    }

    private static async Task MarkTriggeredAsync(Canary canary, AppDbContext db, CancellationToken ct)
    {
        canary.Status = "Triggered";
        canary.TriggeredAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Resolves each canary in this tick's batch to its destination(s) via the canary type's rules
    /// (independently evaluated, so one canary can fan out to several), falling back to the type's
    /// default channels/severity when nothing matches. Canaries are then regrouped by destination —
    /// not just by canary type — since two canaries of the same type can resolve to different
    /// channels or severities and must never be folded into the same dedup batch.
    /// </summary>
    private async Task ProcessAlertGroupAsync(
        CanaryType canaryType, List<Canary> canaries, ConnectorRegistry connectorRegistry, AppDbContext db, CancellationToken ct)
    {
        var destinations = new List<(string Channel, string Severity, Canary Canary)>();

        foreach (var canary in canaries)
        {
            var matchedRules = canaryType.Rules.Where(r => RuleEvaluator.Matches(canary, r)).ToList();

            if (matchedRules.Count == 0)
            {
                destinations.Add((DefaultDestinationChannel, canaryType.Severity, canary));
                continue;
            }

            foreach (var (channel, severity) in matchedRules.Select(r => (r.Channel, r.Severity)).Distinct())
            {
                destinations.Add((channel, severity, canary));
            }
        }

        foreach (var destinationGroup in destinations.GroupBy(d => (d.Channel, d.Severity)))
        {
            var canariesInGroup = destinationGroup.Select(d => d.Canary).ToList();
            await ProcessDestinationGroupAsync(
                canaryType, destinationGroup.Key.Channel, destinationGroup.Key.Severity, canariesInGroup, connectorRegistry, db, ct);
        }
    }

    /// <summary>
    /// Decides what (if anything) to alert for a batch of same-type, same-destination triggers that
    /// all happened in this poll tick. Folds the whole batch into this destination's open alert
    /// window in one shot, so a 1000-canary burst reports its true count in a single alert instead
    /// of "1 triggered" with 999 more silently swallowed.
    /// </summary>
    private async Task ProcessDestinationGroupAsync(
        CanaryType canaryType, string destinationChannel, string severity, List<Canary> canaries,
        ConnectorRegistry connectorRegistry, AppDbContext db, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var mostRecent = canaries[^1];

        // Use the canaries' own TriggeredAt rather than "now" — MarkTriggeredAsync already stamped
        // each one slightly earlier, so a window built from "now" can start after its own example
        // canary triggered, which makes the dashboard link's "since" filter exclude it entirely.
        var earliestTriggeredAt = canaries.Min(c => c.TriggeredAt ?? now);
        var latestTriggeredAt = canaries.Max(c => c.TriggeredAt ?? now);

        if (canaryType.DedupInterval <= 0)
        {
            foreach (var canary in canaries)
            {
                var triggeredAt = canary.TriggeredAt ?? now;
                await SendAlertAsync(
                    canary, canaryType, destinationChannel,
                    new AlertContext(1, triggeredAt, triggeredAt, IsRenotify: false, DashboardUrl: null, severity),
                    connectorRegistry, db, ct);
            }
            return;
        }

        var batch = await db.CanaryAlertBatches
            .FirstOrDefaultAsync(b => b.CanaryTypeId == canaryType.Id && b.Channel == destinationChannel && b.Severity == severity && b.IsOpen, ct);

        // A batch can still be flagged open even after its quiet period has elapsed if the
        // periodic sweep hasn't run since the window closed — without this check, a trigger
        // arriving right on (or just past) the dedup boundary would silently fold into the stale
        // batch as a renotify instead of starting a fresh alert.
        if (batch is not null && (earliestTriggeredAt - batch.LastFailureAt).TotalSeconds >= canaryType.DedupInterval)
        {
            batch.IsOpen = false;
            await db.SaveChangesAsync(ct);
            batch = null;
        }

        if (batch is null)
        {
            batch = new CanaryAlertBatch
            {
                CanaryTypeId = canaryType.Id,
                Channel = destinationChannel,
                Severity = severity,
                OpenedAt = earliestTriggeredAt,
                LastFailureAt = latestTriggeredAt,
                LastNotifiedAt = now,
                Count = canaries.Count,
                CountSinceLastNotify = 0,
                LastCanaryId = mostRecent.Id,
                IsOpen = true,
            };
            db.CanaryAlertBatches.Add(batch);
            await db.SaveChangesAsync(ct);

            await SendAlertAsync(
                mostRecent, canaryType, destinationChannel,
                new AlertContext(canaries.Count, earliestTriggeredAt, latestTriggeredAt, IsRenotify: false, DashboardUrl: null, severity),
                connectorRegistry, db, ct);
            return;
        }

        batch.Count += canaries.Count;
        batch.CountSinceLastNotify += canaries.Count;
        batch.LastFailureAt = latestTriggeredAt;
        batch.LastCanaryId = mostRecent.Id;

        var shouldRenotify = canaryType.RenotifyInterval > 0
            && (now - batch.LastNotifiedAt).TotalSeconds >= canaryType.RenotifyInterval;

        if (!shouldRenotify)
        {
            await db.SaveChangesAsync(ct);
            return;
        }

        var deduplicatedCount = batch.CountSinceLastNotify;
        var windowStart = batch.OpenedAt;
        batch.LastNotifiedAt = now;
        batch.CountSinceLastNotify = 0;
        await db.SaveChangesAsync(ct);

        await SendAlertAsync(
            mostRecent, canaryType, destinationChannel,
            new AlertContext(deduplicatedCount, windowStart, latestTriggeredAt, IsRenotify: true, DashboardUrl: null, severity),
            connectorRegistry, db, ct);
    }

    /// <summary>
    /// Renotifies batches whose renotify interval has elapsed (so a "still firing" summary doesn't
    /// depend on a fresh trigger arriving to check the clock) and closes batches that have gone quiet
    /// for the canary type's dedup interval. Runs every poll tick independent of new triggers, since a
    /// sliding window only ever advances when a new trigger comes in — nothing else would otherwise
    /// close it once failures stop.
    /// </summary>
    private async Task SweepAlertBatchesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var connectorRegistry = scope.ServiceProvider.GetRequiredService<ConnectorRegistry>();

        var now = DateTime.UtcNow;
        var openBatches = await db.CanaryAlertBatches
            .Where(b => b.IsOpen)
            .ToListAsync(ct);

        if (openBatches.Count == 0) return;

        var canaryTypes = await db.CanaryTypes
            .Include(t => t.DefaultChannels)
            .Where(t => openBatches.Select(b => b.CanaryTypeId).Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, ct);

        var closedAny = false;
        foreach (var batch in openBatches)
        {
            if (!canaryTypes.TryGetValue(batch.CanaryTypeId, out var canaryType)) continue;

            var shouldRenotify = batch.CountSinceLastNotify > 0
                && canaryType.RenotifyInterval > 0
                && (now - batch.LastNotifiedAt).TotalSeconds >= canaryType.RenotifyInterval;

            if (shouldRenotify)
            {
                var mostRecent = await db.Canaries.FindAsync([batch.LastCanaryId], ct);
                if (mostRecent is not null)
                {
                    var deduplicatedCount = batch.CountSinceLastNotify;
                    var windowStart = batch.OpenedAt;
                    batch.LastNotifiedAt = now;
                    batch.CountSinceLastNotify = 0;
                    await db.SaveChangesAsync(ct);

                    await SendAlertAsync(
                        mostRecent, canaryType, batch.Channel,
                        new AlertContext(deduplicatedCount, windowStart, batch.LastFailureAt, IsRenotify: true, DashboardUrl: null, batch.Severity),
                        connectorRegistry, db, ct);
                }
            }

            if ((now - batch.LastFailureAt).TotalSeconds >= canaryType.DedupInterval)
            {
                batch.IsOpen = false;
                closedAny = true;
            }
        }

        if (closedAny)
        {
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task SendAlertAsync(
        Canary canary, CanaryType canaryType, string destinationChannel, AlertContext alertContext,
        ConnectorRegistry connectorRegistry, AppDbContext db, CancellationToken ct)
    {
        var adminPortalUrl = (await db.Settings.SingleAsync(ct)).AdminPortalUrl;

        // Only link to the dashboard's time-range view when there's actually more than one canary
        // behind this alert — for a lone instance, the per-canary link below already covers it, and
        // a single-instant since==until window is needlessly exposed to exact-timestamp collisions
        // with an unrelated canary of the same type.
        var dashboardUrl = string.IsNullOrWhiteSpace(adminPortalUrl) || alertContext.DeduplicatedCount <= 1
            ? null
            : BuildDashboardUrl(adminPortalUrl, canaryType.Name, alertContext.WindowStart, alertContext.WindowEnd);
        alertContext = alertContext with { DashboardUrl = dashboardUrl };

        // No rule matched (or dedup is disabled and we're at the type's default) → send to every
        // configured default channel, same as before rules existed. A matched rule instead names a
        // specific channel by name (picked from the same channel list as the default-channels table
        // in the editor), so it routes there directly regardless of whether that channel is also one
        // of this type's defaults.
        List<Channel> targetChannels;
        if (destinationChannel == DefaultDestinationChannel)
        {
            var defaultChannelIds = canaryType.DefaultChannels.Select(dc => dc.ChannelId).ToList();
            targetChannels = await db.Channels.Where(c => defaultChannelIds.Contains(c.Id)).ToListAsync(ct);
        }
        else
        {
            targetChannels = await db.Channels.Where(c => c.Name == destinationChannel).ToListAsync(ct);
        }

        if (targetChannels.Count == 0)
        {
            logger.LogWarning(
                "A rule for canary type '{CanaryType}' resolved to channel '{Channel}', but no channel with that name exists; skipping.",
                canaryType.Name, destinationChannel);
            return;
        }

        foreach (var channel in targetChannels)
        {
            var connector = connectorRegistry.Resolve(channel.Type);
            if (connector is null)
            {
                logger.LogWarning("No connector registered for channel type '{ChannelType}'.", channel.Type);
                continue;
            }

            try
            {
                var config = JsonSerializer.Deserialize<JsonElement>(channel.Config);
                await connector.SendAsync(config, canary, canaryType, adminPortalUrl, alertContext, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(
                    ex, "Failed to notify channel '{ChannelName}' for canary {CanaryType}/{ReferenceId}.",
                    channel.Name, canary.CanaryType, canary.ReferenceId);
            }
        }
    }

    private static string BuildDashboardUrl(string adminPortalUrl, string canaryTypeName, DateTime since, DateTime until)
    {
        // since/until are the exact TriggeredAt values of the first and last canary actually folded
        // into this alert, and the controller's filter is inclusive (>=/<=), so no padding is needed
        // here — padding previously caused unrelated canaries that merely expired nearby in time (but
        // weren't part of this batch) to show up in the linked dashboard view.
        var sinceParam = since.ToString("o");
        var untilParam = until.ToString("o");
        return $"{adminPortalUrl}/canaries"
            + $"?canaryType={Uri.EscapeDataString(canaryTypeName)}&since={Uri.EscapeDataString(sinceParam)}&until={Uri.EscapeDataString(untilParam)}";
    }
}
