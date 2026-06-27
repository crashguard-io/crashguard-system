using Crashguard.Client;
using Crashguard.Client.Models;
using Crashguard.Common.DTOs;
using Crashguard.Sim.Models;

namespace Crashguard.Sim.Services;

public class Service(EngineClient engineClient, CrashguardClient crashguardClient, ILogger<Service> logger) : BackgroundService
{
    private const string OpsChannelName = "ops-crashguard-critical";
    private const int CanaryTypeCount = 10;
    private const double ResolveProbability = 0.6;
    private const double SpawnsPerMinute = 10;
    private const int WaveMinSize = 20;
    private const int WaveMaxSize = 30;
    private const double WavesPerHour = 12;

    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan CheckpointInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan AverageSpawnInterval = TimeSpan.FromMinutes(1) / SpawnsPerMinute;
    private static readonly TimeSpan AverageWaveInterval = TimeSpan.FromHours(1) / WavesPerHour;

    private readonly List<SimulatedCanary> _canaries = [];
    private readonly Random _random = new();
    private DateTime _nextSpawnAt = DateTime.UtcNow;
    private DateTime _nextWaveAt = DateTime.UtcNow + TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Crashguard sim started.");

        try
        {
            await EnsureCanaryTypesAsync(stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error while setting up sim canary types.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error while ticking sim canaries.");
            }

            await Task.Delay(TickInterval, stoppingToken);
        }

        logger.LogInformation("Crashguard sim stopped.");
    }

    private async Task EnsureCanaryTypesAsync(CancellationToken ct)
    {
        var channels = await engineClient.GetChannelsAsync(ct);
        var opsChannel = channels.FirstOrDefault(c => c.Name == OpsChannelName);
        if (opsChannel is null)
        {
            logger.LogWarning(
                "No channel named '{ChannelName}' is configured; sim canary types will be created without a default channel.",
                OpsChannelName);
        }

        var defaultChannelIds = opsChannel is null ? new List<int>() : new List<int> { opsChannel.Id };

        var existingTypes = await engineClient.GetCanaryTypesAsync(ct);
        var existingNames = existingTypes.Select(t => t.Name).ToHashSet();

        for (var i = 1; i <= CanaryTypeCount; i++)
        {
            var name = $"crashguard-sim-{i}";
            if (existingNames.Contains(name))
            {
                logger.LogInformation("Canary type '{Name}' already exists; skipping.", name);
                continue;
            }

            var request = new CreateCanaryTypeRequest
            {
                Name = name,
                Timeout = _random.Next(60, 601),
                ExtendLimit = 0,
                Severity = "critical",
                VerifierUrl = null,
                DefaultChannelIds = defaultChannelIds,
            };

            await engineClient.CreateCanaryTypeAsync(request, ct);
            logger.LogInformation("Created canary type '{Name}' with timeout {Timeout}s.", name, request.Timeout);
        }
    }

    private async Task SpawnSimulatedCanaryAsync(CancellationToken ct)
    {
        var canaryType = $"crashguard-sim-{_random.Next(1, CanaryTypeCount + 1)}";
        var referenceId = $"sim-{Guid.NewGuid():N}";

        var canaryDto = await crashguardClient.CreateCanaryAsync(
            new CreateCanaryRequest { CanaryType = canaryType, ReferenceId = referenceId },
            ct);

        if (canaryDto is null) return;

        // Canaries chosen to resolve do so at a random point within their timeout window, so
        // they succeed before expiring; the rest are left alone so the engine's overdue check
        // triggers them as failures.
        var willResolve = _random.NextDouble() < ResolveProbability;
        DateTime? resolveAt = willResolve
            ? canaryDto.StartedAt.AddSeconds(canaryDto.Timeout * (0.2 + _random.NextDouble() * 0.6))
            : null;

        _canaries.Add(new SimulatedCanary
        {
            CanaryType = canaryType,
            ReferenceId = referenceId,
            StartedAt = canaryDto.StartedAt,
            ExpiresAt = canaryDto.ExpiresAt,
            ResolveAt = resolveAt,
        });

        logger.LogInformation("Spawned canary {CanaryType}/{ReferenceId}, resolves={WillResolve}.", canaryType, referenceId, willResolve);
    }

    private DateTime NextSpawnDelay(DateTime from)
    {
        // Jitter the interval between +/-50% of the average so spawns feel random rather than metronomic.
        var jitterFactor = 0.5 + _random.NextDouble();
        return from + TimeSpan.FromTicks((long)(AverageSpawnInterval.Ticks * jitterFactor));
    }

    private DateTime NextWaveDelay(DateTime from)
    {
        var jitterFactor = 0.5 + _random.NextDouble();
        return from + TimeSpan.FromTicks((long)(AverageWaveInterval.Ticks * jitterFactor));
    }

    // Spawns a batch of canaries of the same type, fired off concurrently with no resolution,
    // so they all expire within the same engine overdue-check cycle and exercise alert dedup.
    private async Task SpawnWaveAsync(CancellationToken ct)
    {
        var canaryType = $"crashguard-sim-{_random.Next(1, CanaryTypeCount + 1)}";
        var waveSize = _random.Next(WaveMinSize, WaveMaxSize + 1);

        logger.LogInformation("Spawning a wave of {WaveSize} canaries of type {CanaryType}.", waveSize, canaryType);

        var tasks = Enumerable.Range(0, waveSize).Select(async _ =>
        {
            var referenceId = $"sim-wave-{Guid.NewGuid():N}";
            CanaryDto? canaryDto;
            try
            {
                canaryDto = await crashguardClient.CreateCanaryAsync(
                    new CreateCanaryRequest { CanaryType = canaryType, ReferenceId = referenceId },
                    ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Failed to spawn wave canary {CanaryType}/{ReferenceId}.", canaryType, referenceId);
                return;
            }

            if (canaryDto is null) return;

            lock (_canaries)
            {
                // No ResolveAt: wave canaries are left to expire together so the engine has to
                // dedupe a burst of failures for the same canary type instead of resolving cleanly.
                _canaries.Add(new SimulatedCanary
                {
                    CanaryType = canaryType,
                    ReferenceId = referenceId,
                    StartedAt = canaryDto.StartedAt,
                    ExpiresAt = canaryDto.ExpiresAt,
                    ResolveAt = null,
                });
            }
        });

        await Task.WhenAll(tasks);
    }

    private async Task TickAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        while (now >= _nextSpawnAt)
        {
            await SpawnSimulatedCanaryAsync(ct);
            _nextSpawnAt = NextSpawnDelay(_nextSpawnAt);
        }

        while (now >= _nextWaveAt)
        {
            await SpawnWaveAsync(ct);
            _nextWaveAt = NextWaveDelay(_nextWaveAt);
        }

        foreach (var canary in _canaries)
        {
            if (canary.Finished) continue;

            var dueCheckpoints = (int)((now - canary.StartedAt) / CheckpointInterval);
            while (canary.CheckpointsSent < dueCheckpoints && !canary.Finished)
            {
                var step = canary.CheckpointsSent + 1;
                try
                {
                    await crashguardClient.CheckpointCanaryAsync(canary.CanaryType, canary.ReferenceId, $"checkpoint-step-{step}", cancellationToken: ct);
                    canary.CheckpointsSent = step;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Failed to write checkpoint for canary {CanaryType}/{ReferenceId}.", canary.CanaryType, canary.ReferenceId);
                    break;
                }
            }

            if (canary.ResolveAt is { } resolveAt && now >= resolveAt)
            {
                try
                {
                    await crashguardClient.ResolveCanaryAsync(canary.CanaryType, canary.ReferenceId, ct);
                    canary.Finished = true;
                    logger.LogInformation("Resolved canary {CanaryType}/{ReferenceId}.", canary.CanaryType, canary.ReferenceId);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Failed to resolve canary {CanaryType}/{ReferenceId}.", canary.CanaryType, canary.ReferenceId);
                }
            }
            else if (canary.ResolveAt is null && now >= canary.ExpiresAt)
            {
                canary.Finished = true;
            }
        }

        _canaries.RemoveAll(c => c.Finished);
    }

    private sealed class SimulatedCanary
    {
        public required string CanaryType { get; init; }
        public required string ReferenceId { get; init; }
        public DateTime StartedAt { get; init; }
        public DateTime ExpiresAt { get; init; }
        public DateTime? ResolveAt { get; init; }
        public int CheckpointsSent { get; set; }
        public bool Finished { get; set; }
    }
}
