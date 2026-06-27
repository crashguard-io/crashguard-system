using Crashguard.Client;
using Crashguard.Client.Models;
using Crashguard.Sim.Models;

namespace Crashguard.Sim.Services;

public class LoadTestService(
    EngineClient engineClient,
    CrashguardClient crashguardClient,
    LoadTestOptions options,
    IHostApplicationLifetime appLifetime,
    ILogger<LoadTestService> logger) : BackgroundService
{
    private const string CanaryTypeName = "crashguard-sim-load-1";
    private const int CanaryTimeoutSeconds = 30;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxWait = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RunLoadTestAsync(stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Load test failed.");
        }
        finally
        {
            appLifetime.StopApplication();
        }
    }

    private async Task RunLoadTestAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting load test with {Count} canaries.", options.CanaryCount);

        await EnsureCanaryTypeAsync(ct);

        var referenceIds = Enumerable.Range(1, options.CanaryCount)
            .Select(i => $"load-{Guid.NewGuid():N}-{i}")
            .ToList();

        var createTasks = referenceIds.Select(refId => crashguardClient.CreateCanaryAsync(
            new CreateCanaryRequest { CanaryType = CanaryTypeName, ReferenceId = refId }, ct));
        var created = await Task.WhenAll(createTasks);

        // Anchor the batch's "processing" window on ExpiresAt rather than creation time, since the
        // 30s timeout is just the canaries waiting to become overdue — the engine doesn't start
        // calling verifiers until then, so that wait shouldn't count toward verifier/resolve time.
        var earliestExpiresAt = created.Where(c => c is not null).Min(c => c!.ExpiresAt);

        logger.LogInformation("Created {Count} canaries; waiting for them to resolve.", options.CanaryCount);

        var (resolvedCount, triggeredCount, latestFinishedAt) = await WaitForCompletionAsync(referenceIds, ct);

        var unresolvedCount = options.CanaryCount - resolvedCount - triggeredCount;
        var processingSeconds = latestFinishedAt > earliestExpiresAt
            ? (latestFinishedAt - earliestExpiresAt).TotalSeconds
            : 0;
        var rate = processingSeconds > 0 ? options.CanaryCount / processingSeconds : 0;

        logger.LogInformation(
            "Load test complete: {Count} canaries processed in {ProcessingSeconds:F1}s ({Rate:F2}/s). Resolved={Resolved}, Triggered={Triggered}, Unresolved={Unresolved}.",
            options.CanaryCount, processingSeconds, rate, resolvedCount, triggeredCount, unresolvedCount);
    }

    private async Task<(int ResolvedCount, int TriggeredCount, DateTime LatestFinishedAt)> WaitForCompletionAsync(
        List<string> referenceIds, CancellationToken ct)
    {
        var pending = new HashSet<string>(referenceIds);
        var resolvedCount = 0;
        var triggeredCount = 0;
        var latestFinishedAt = DateTime.MinValue;
        var deadline = DateTime.UtcNow + MaxWait;

        while (pending.Count > 0 && !ct.IsCancellationRequested && DateTime.UtcNow < deadline)
        {
            await Task.Delay(PollInterval, ct);

            var statusTasks = pending.Select(async refId =>
            {
                try
                {
                    var canary = await crashguardClient.GetCanaryAsync(CanaryTypeName, refId, ct);
                    return (refId, canary);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Failed to poll status for canary {ReferenceId}.", refId);
                    return (refId, null);
                }
            });

            foreach (var (refId, canary) in await Task.WhenAll(statusTasks))
            {
                if (canary?.Status is not ("Resolved" or "Triggered")) continue;

                pending.Remove(refId);
                if (canary.Status == "Resolved")
                {
                    resolvedCount++;
                    if (canary.ResolvedAt is { } resolvedAt && resolvedAt > latestFinishedAt) latestFinishedAt = resolvedAt;
                }
                else
                {
                    triggeredCount++;
                    if (canary.TriggeredAt is { } triggeredAt && triggeredAt > latestFinishedAt) latestFinishedAt = triggeredAt;
                }
            }
        }

        if (pending.Count > 0)
        {
            logger.LogWarning("Gave up waiting for {Count} canaries after {Minutes} minutes.", pending.Count, MaxWait.TotalMinutes);
        }

        return (resolvedCount, triggeredCount, latestFinishedAt);
    }

    private async Task EnsureCanaryTypeAsync(CancellationToken ct)
    {
        var verifierUrl = $"http://localhost:{options.ApiPort}/api/verify?delayMs={options.VerifyDelayMs}";

        var existingTypes = await engineClient.GetCanaryTypesAsync(ct);
        var existing = existingTypes.FirstOrDefault(t => t.Name == CanaryTypeName);

        if (existing is not null)
        {
            var updateRequest = new UpdateCanaryTypeRequest
            {
                Name = CanaryTypeName,
                Timeout = CanaryTimeoutSeconds,
                ExtendLimit = 0,
                Severity = "critical",
                VerifierUrl = verifierUrl,
                DefaultChannelIds = existing.DefaultChannelIds,
            };

            await engineClient.UpdateCanaryTypeAsync(existing.Id, updateRequest, ct);
            logger.LogInformation("Updated canary type '{Name}' with verifier '{VerifierUrl}'.", CanaryTypeName, verifierUrl);
            return;
        }

        var createRequest = new CreateCanaryTypeRequest
        {
            Name = CanaryTypeName,
            Timeout = CanaryTimeoutSeconds,
            ExtendLimit = 0,
            Severity = "critical",
            VerifierUrl = verifierUrl,
            DefaultChannelIds = [],
        };

        await engineClient.CreateCanaryTypeAsync(createRequest, ct);
        logger.LogInformation("Created canary type '{Name}' with verifier '{VerifierUrl}'.", CanaryTypeName, verifierUrl);
    }
}
