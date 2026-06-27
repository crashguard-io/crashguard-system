using Crashguard.Common.DTOs;
using Crashguard.Engine.Data;
using Crashguard.Engine.Models;
using Crashguard.Engine.Models.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crashguard.Engine.Controllers;

[ApiController]
[Route("api/canary-types")]
public class CanaryTypesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CanaryTypeDto>>> List(CancellationToken ct)
    {
        var canaryTypes = await db.CanaryTypes.Include(c => c.Rules).Include(c => c.DefaultChannels).AsSplitQuery().OrderBy(c => c.Name).ToListAsync(ct);
        return Ok(canaryTypes.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CanaryTypeDto>> Get(int id, CancellationToken ct)
    {
        var canaryType = await db.CanaryTypes.Include(c => c.Rules).Include(c => c.DefaultChannels).AsSplitQuery().FirstOrDefaultAsync(c => c.Id == id, ct);
        if (canaryType is null) return NotFound();

        return Ok(ToDto(canaryType));
    }

    [HttpGet("status")]
    public async Task<ActionResult<CanaryTypeAggregateStatusDto>> AggregateStatus([FromQuery] string names, [FromQuery] DateTime? since, CancellationToken ct)
    {
        var nameList = names
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        if (nameList.Count == 0) return BadRequest("At least one canary type name is required.");

        var canaries = await db.Canaries.Where(c => nameList.Contains(c.CanaryType)).ToListAsync(ct);

        var now = DateTime.UtcNow;
        var sinceUtc = since ?? DateTime.MinValue;

        bool IsAtRisk(Canary c, DateTime asOf) =>
            c.Timeout > 0 && asOf >= c.StartedAt && (double)(c.Timeout - (c.ExpiresAt - asOf).TotalSeconds) / c.Timeout >= 0.8;

        var pendingCount = canaries.Count(c => c.StartedAt > sinceUtc);
        var atRiskCount = canaries.Count(c =>
            c.Status == "Pending" && IsAtRisk(c, now) && !IsAtRisk(c, sinceUtc));
        var triggeredCount = canaries.Count(c =>
            c.Status == "Triggered" && c.TriggeredAt.HasValue && c.TriggeredAt.Value > sinceUtc);
        var resolvedCount = canaries.Count(c =>
            c.Status == "Resolved" && c.ResolvedAt.HasValue && c.ResolvedAt.Value > sinceUtc);

        return Ok(new CanaryTypeAggregateStatusDto
        {
            PendingCount = pendingCount,
            ResolvedCount = resolvedCount,
            AtRiskCount = atRiskCount,
            TriggeredCount = triggeredCount,
        });
    }

    [HttpGet("{name}/status")]
    public async Task<ActionResult<CanaryTypeStatusDto>> Status(string name, [FromQuery] DateTime? since, CancellationToken ct)
    {
        var canaryType = await db.CanaryTypes.FirstOrDefaultAsync(c => c.Name == name, ct);
        if (canaryType is null) return NotFound($"No canary type named '{name}' is configured.");

        var canaries = await db.Canaries.Where(c => c.CanaryType == name).ToListAsync(ct);

        var now = DateTime.UtcNow;
        var pending = canaries.Where(c => c.Status == "Pending").ToList();
        var atRiskCount = pending.Count(c =>
            c.Timeout > 0 && (double)(c.Timeout - (c.ExpiresAt - now).TotalSeconds) / c.Timeout >= 0.8);

        var sinceUtc = since ?? DateTime.MinValue;
        var triggeredSinceCount = canaries.Count(c =>
            c.Status == "Triggered" && c.TriggeredAt.HasValue && c.TriggeredAt.Value > sinceUtc);

        return Ok(new CanaryTypeStatusDto
        {
            CanaryType = name,
            PendingCount = pending.Count,
            AtRiskCount = atRiskCount,
            TriggeredSinceCount = triggeredSinceCount,
        });
    }

    [HttpGet("{name}/history")]
    public async Task<ActionResult<CanaryTypeHistoryDto>> History(
        string name,
        [FromQuery] DateTime? since,
        [FromQuery] int bucketSeconds = 300,
        CancellationToken ct = default)
    {
        if (bucketSeconds <= 0) return BadRequest("bucketSeconds must be positive.");

        var canaryType = await db.CanaryTypes.FirstOrDefaultAsync(c => c.Name == name, ct);
        if (canaryType is null) return NotFound($"No canary type named '{name}' is configured.");

        var now = DateTime.UtcNow;
        var sinceUtc = since ?? now.AddSeconds(-bucketSeconds * 12);

        var canaries = await db.Canaries.Where(c => c.CanaryType == name).ToListAsync(ct);

        var bucketCount = Math.Max(1, (int)Math.Ceiling((now - sinceUtc).TotalSeconds / bucketSeconds));
        var buckets = new List<CanaryTypeHistoryBucketDto>(bucketCount);

        for (var i = 0; i < bucketCount; i++)
        {
            var bucketStart = sinceUtc.AddSeconds(i * bucketSeconds);
            var bucketEnd = bucketStart.AddSeconds(bucketSeconds);

            var resolvedInBucket = canaries
                .Where(c => c.ResolvedAt.HasValue && c.ResolvedAt.Value >= bucketStart && c.ResolvedAt.Value < bucketEnd)
                .ToList();

            var resolutionTimes = resolvedInBucket
                .Select(c => (c.ResolvedAt!.Value - (c.TriggeredAt ?? c.StartedAt)).TotalSeconds)
                .ToList();

            buckets.Add(new CanaryTypeHistoryBucketDto
            {
                BucketStart = bucketStart,
                TriggeredCount = canaries.Count(c => c.TriggeredAt.HasValue && c.TriggeredAt.Value >= bucketStart && c.TriggeredAt.Value < bucketEnd),
                ResolvedCount = resolvedInBucket.Count,
                PendingCount = canaries.Count(c =>
                    c.StartedAt < bucketEnd &&
                    (!c.TriggeredAt.HasValue || c.TriggeredAt.Value >= bucketEnd) &&
                    (!c.ResolvedAt.HasValue || c.ResolvedAt.Value >= bucketEnd)),
                AvgResolutionSeconds = resolutionTimes.Count > 0 ? resolutionTimes.Average() : null,
            });
        }

        return Ok(new CanaryTypeHistoryDto { CanaryType = name, Buckets = buckets });
    }

    [HttpGet("{name}/canaries")]
    public async Task<ActionResult<List<CanaryDto>>> ListCanaries(
        string name,
        [FromQuery] string? status,
        [FromQuery] DateTime? since,
        [FromQuery] DateTime? until,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var canaryType = await db.CanaryTypes.FirstOrDefaultAsync(c => c.Name == name, ct);
        if (canaryType is null) return NotFound($"No canary type named '{name}' is configured.");

        var query = db.Canaries.Where(c => c.CanaryType == name);
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status);
        }
        if (since.HasValue)
        {
            query = query.Where(c => c.TriggeredAt.HasValue && c.TriggeredAt.Value >= since.Value);
        }
        if (until.HasValue)
        {
            query = query.Where(c => c.TriggeredAt.HasValue && c.TriggeredAt.Value <= until.Value);
        }

        var canaries = await query
            .OrderByDescending(c => c.TriggeredAt ?? c.StartedAt)
            .Take(limit)
            .ToListAsync(ct);

        return Ok(canaries.Select(ToCanaryDto));
    }

    [HttpPost]
    public async Task<ActionResult<CanaryTypeDto>> Create(CreateCanaryTypeRequest request, CancellationToken ct)
    {
        if (await FindInvalidChannelId(request.DefaultChannelIds, ct) is { } invalidId)
        {
            return BadRequest($"No channel with id {invalidId} exists.");
        }

        var canaryType = new CanaryType
        {
            Name = request.Name,
            Timeout = request.Timeout,
            ExtendLimit = request.ExtendLimit,
            DedupInterval = request.DedupInterval,
            RenotifyInterval = request.RenotifyInterval,
            Severity = request.Severity,
            MetadataSchema = request.MetadataSchema,
            VerifierUrl = request.VerifierUrl,
            CreatedAt = DateTime.UtcNow,
            Rules = request.Rules.Select(ToRule).ToList(),
            DefaultChannels = request.DefaultChannelIds.Select(ToCanaryTypeChannel).ToList(),
        };

        db.CanaryTypes.Add(canaryType);
        await db.SaveChangesAsync(ct);

        return Ok(ToDto(canaryType));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CanaryTypeDto>> Update(int id, UpdateCanaryTypeRequest request, CancellationToken ct)
    {
        var canaryType = await db.CanaryTypes.Include(c => c.Rules).Include(c => c.DefaultChannels).AsSplitQuery().FirstOrDefaultAsync(c => c.Id == id, ct);
        if (canaryType is null) return NotFound();

        if (await FindInvalidChannelId(request.DefaultChannelIds, ct) is { } invalidId)
        {
            return BadRequest($"No channel with id {invalidId} exists.");
        }

        canaryType.Name = request.Name;
        canaryType.Timeout = request.Timeout;
        canaryType.ExtendLimit = request.ExtendLimit;
        canaryType.DedupInterval = request.DedupInterval;
        canaryType.RenotifyInterval = request.RenotifyInterval;
        canaryType.Severity = request.Severity;
        canaryType.MetadataSchema = request.MetadataSchema;
        canaryType.VerifierUrl = request.VerifierUrl;

        canaryType.Rules.Clear();
        foreach (var rule in request.Rules.Select(ToRule))
        {
            canaryType.Rules.Add(rule);
        }

        canaryType.DefaultChannels.Clear();
        foreach (var channel in request.DefaultChannelIds.Select(ToCanaryTypeChannel))
        {
            canaryType.DefaultChannels.Add(channel);
        }

        await db.SaveChangesAsync(ct);

        return Ok(ToDto(canaryType));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var canaryType = await db.CanaryTypes.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (canaryType is null) return NotFound();

        db.CanaryTypes.Remove(canaryType);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    private async Task<int?> FindInvalidChannelId(List<int> channelIds, CancellationToken ct)
    {
        if (channelIds.Count == 0) return null;

        var existingIds = await db.Channels.Where(c => channelIds.Contains(c.Id)).Select(c => c.Id).ToHashSetAsync(ct);
        foreach (var id in channelIds)
        {
            if (!existingIds.Contains(id)) return id;
        }
        return null;
    }

    private static CanaryDto ToCanaryDto(Canary canary) => new()
    {
        Id = canary.Id,
        CanaryType = canary.CanaryType,
        ReferenceId = canary.ReferenceId,
        Status = canary.Status,
        StartedAt = canary.StartedAt,
        ResolvedAt = canary.ResolvedAt,
        TriggeredAt = canary.TriggeredAt,
        Timeout = canary.Timeout,
        ExpiresAt = canary.ExpiresAt,
        Metadata = canary.Metadata is not null ? System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(canary.Metadata) : null,
    };

    private static CanaryTypeChannel ToCanaryTypeChannel(int channelId) => new() { ChannelId = channelId };

    private static CanaryTypeRule ToRule(CanaryTypeRuleRequest request) => new()
    {
        Field = request.Field,
        Operator = request.Operator,
        Value = request.Value,
        Severity = request.Severity,
        Channel = request.Channel,
    };

    private static CanaryTypeRuleDto ToDto(CanaryTypeRule rule) => new()
    {
        Id = rule.Id,
        Field = rule.Field,
        Operator = rule.Operator,
        Value = rule.Value,
        Severity = rule.Severity,
        Channel = rule.Channel,
    };

    private static CanaryTypeDto ToDto(CanaryType canaryType) => new()
    {
        Id = canaryType.Id,
        Name = canaryType.Name,
        Timeout = canaryType.Timeout,
        ExtendLimit = canaryType.ExtendLimit,
        DedupInterval = canaryType.DedupInterval,
        RenotifyInterval = canaryType.RenotifyInterval,
        Severity = canaryType.Severity,
        MetadataSchema = canaryType.MetadataSchema,
        VerifierUrl = canaryType.VerifierUrl,
        CreatedAt = canaryType.CreatedAt,
        Rules = canaryType.Rules.Select(ToDto).ToList(),
        DefaultChannelIds = canaryType.DefaultChannels.Select(c => c.ChannelId).ToList(),
    };
}
