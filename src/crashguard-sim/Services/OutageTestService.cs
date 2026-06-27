using Crashguard.Client;
using Crashguard.Client.Models;
using Crashguard.Sim.Models;

namespace Crashguard.Sim.Services;

// Simulates a sustained outage to exercise a canary type's dedup/renotify alerting: a wave of
// triggered canaries every minute, sized so the dedup interval spans the whole run, then one
// final wave sent only after that interval has elapsed so it lands as a fresh alert instead of
// folding into the batch the regular waves kept open.
public class OutageTestService(
    EngineClient engineClient,
    CrashguardClient crashguardClient,
    OutageTestOptions options,
    IHostApplicationLifetime appLifetime,
    ILogger<OutageTestService> logger) : BackgroundService
{
    private const string CanaryTypeName = "crashguard-sim-outage-1";
    private const string OpsChannelName = "ops-crashguard-critical";
    private const int CanariesPerBatch = 10;
    private const int CanaryTimeoutSeconds = 60;
    private const int RenotifyIntervalSeconds = 60;

    private static readonly TimeSpan WaveInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RunOutageTestAsync(stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Outage test failed.");
        }
        finally
        {
            appLifetime.StopApplication();
        }
    }

    private async Task RunOutageTestAsync(CancellationToken ct)
    {
        // Sized to the regular wave count (the final batch deliberately doesn't count toward it)
        // so the dedup window comfortably spans every regular wave's trigger.
        var dedupInterval = TimeSpan.FromMinutes(options.BatchCount);

        logger.LogInformation(
            "Starting outage simulation: {BatchCount} waves of {CanariesPerBatch} canaries one minute apart, " +
            "dedup interval set to {DedupMinutes} minute(s), final wave sent after that interval elapses.",
            options.BatchCount, CanariesPerBatch, options.BatchCount);

        await EnsureCanaryTypeAsync(dedupInterval, ct);

        var allReferenceIds = new List<string>();

        for (var batch = 1; batch <= options.BatchCount; batch++)
        {
            if (batch > 1)
            {
                await Task.Delay(WaveInterval, ct);
            }

            var referenceIds = await CreateBatchAsync($"outage-batch{batch}", ct);
            allReferenceIds.AddRange(referenceIds);
            logger.LogInformation("Sent wave {Batch}/{BatchCount}; expect it to trigger in about {Timeout}s.", batch, options.BatchCount, CanaryTimeoutSeconds);
        }

        // Wait out the full dedup interval (not counted as one of the regular waves) so the open
        // alert batch closes before the final wave arrives.
        await Task.Delay(dedupInterval, ct);

        var finalReferenceIds = await CreateBatchAsync("outage-final", ct);
        allReferenceIds.AddRange(finalReferenceIds);
        logger.LogInformation("Sent final wave; expect it to trigger as a fresh alert after the prior batch has closed.");

        logger.LogInformation(
            "All waves sent. Watch the '{Channel}' Slack channel for triggered alerts, dedup batching, and renotifications.",
            OpsChannelName);

        var maxWait = TimeSpan.FromSeconds(CanaryTimeoutSeconds) + TimeSpan.FromMinutes(2);
        var triggeredCount = await WaitForTriggeredAsync(allReferenceIds, maxWait, ct);

        logger.LogInformation("Outage simulation complete: {Triggered}/{Total} canaries triggered.", triggeredCount, allReferenceIds.Count);
    }

    private async Task<List<string>> CreateBatchAsync(string referenceIdPrefix, CancellationToken ct)
    {
        var referenceIds = Enumerable.Range(1, CanariesPerBatch)
            .Select(i => $"{referenceIdPrefix}-{Guid.NewGuid():N}-{i}")
            .ToList();

        var createTasks = referenceIds.Select(refId => crashguardClient.CreateCanaryAsync(
            new CreateCanaryRequest { CanaryType = CanaryTypeName, ReferenceId = refId }, ct));
        await Task.WhenAll(createTasks);

        return referenceIds;
    }

    private async Task<int> WaitForTriggeredAsync(List<string> referenceIds, TimeSpan maxWait, CancellationToken ct)
    {
        var pending = new HashSet<string>(referenceIds);
        var triggeredCount = 0;
        var deadline = DateTime.UtcNow + maxWait;

        while (pending.Count > 0 && !ct.IsCancellationRequested && DateTime.UtcNow < deadline)
        {
            await Task.Delay(PollInterval, ct);

            var statusTasks = pending.Select(async refId =>
            {
                try
                {
                    var canary = await crashguardClient.GetCanaryAsync(CanaryTypeName, refId, ct);
                    return (refId, canary?.Status);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Failed to poll status for canary {ReferenceId}.", refId);
                    return (refId, (string?)null);
                }
            });

            foreach (var (refId, status) in await Task.WhenAll(statusTasks))
            {
                if (status != "Triggered") continue;

                pending.Remove(refId);
                triggeredCount++;
            }
        }

        if (pending.Count > 0)
        {
            logger.LogWarning("Gave up waiting for {Count} canaries to trigger after {Minutes:F0} minutes.", pending.Count, maxWait.TotalMinutes);
        }

        return triggeredCount;
    }

    private async Task EnsureCanaryTypeAsync(TimeSpan dedupInterval, CancellationToken ct)
    {
        var dedupIntervalSeconds = (int)dedupInterval.TotalSeconds;

        var existingTypes = await engineClient.GetCanaryTypesAsync(ct);
        var existing = existingTypes.FirstOrDefault(t => t.Name == CanaryTypeName);

        if (existing is not null)
        {
            var updateRequest = new UpdateCanaryTypeRequest
            {
                Name = CanaryTypeName,
                Timeout = CanaryTimeoutSeconds,
                ExtendLimit = 0,
                DedupInterval = dedupIntervalSeconds,
                RenotifyInterval = RenotifyIntervalSeconds,
                Severity = "critical",
                VerifierUrl = null,
                DefaultChannelIds = existing.DefaultChannelIds,
            };

            await engineClient.UpdateCanaryTypeAsync(existing.Id, updateRequest, ct);
            logger.LogInformation("Updated canary type '{Name}' dedup interval to {Seconds}s.", CanaryTypeName, dedupIntervalSeconds);
            return;
        }

        var channels = await engineClient.GetChannelsAsync(ct);
        var opsChannel = channels.FirstOrDefault(c => c.Name == OpsChannelName);
        if (opsChannel is null)
        {
            logger.LogWarning(
                "No channel named '{ChannelName}' is configured; outage canary type will be created without a default channel.",
                OpsChannelName);
        }

        var defaultChannelIds = opsChannel is null ? [] : new List<int> { opsChannel.Id };

        var createRequest = new CreateCanaryTypeRequest
        {
            Name = CanaryTypeName,
            Timeout = CanaryTimeoutSeconds,
            ExtendLimit = 0,
            DedupInterval = dedupIntervalSeconds,
            RenotifyInterval = RenotifyIntervalSeconds,
            Severity = "critical",
            VerifierUrl = null,
            DefaultChannelIds = defaultChannelIds,
        };

        await engineClient.CreateCanaryTypeAsync(createRequest, ct);
        logger.LogInformation("Created canary type '{Name}' with dedup interval {Seconds}s.", CanaryTypeName, dedupIntervalSeconds);
    }
}
