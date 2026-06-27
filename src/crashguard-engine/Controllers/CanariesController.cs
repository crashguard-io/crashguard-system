using System.Text.Json;
using Crashguard.Common.DTOs;
using Crashguard.Engine.Data;
using Crashguard.Engine.Models;
using Crashguard.Engine.Models.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crashguard.Engine.Controllers;

[ApiController]
[Route("api/canaries")]
public class CanariesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<CanaryDto>>> List(
        [FromQuery] string? status,
        [FromQuery] string? canaryType,
        [FromQuery] DateTime? since,
        [FromQuery] DateTime? until,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Canaries.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status);
        }
        if (!string.IsNullOrWhiteSpace(canaryType))
        {
            query = query.Where(c => c.CanaryType == canaryType);
        }
        if (since.HasValue)
        {
            query = query.Where(c => c.TriggeredAt.HasValue && c.TriggeredAt.Value >= since.Value);
        }
        if (until.HasValue)
        {
            query = query.Where(c => c.TriggeredAt.HasValue && c.TriggeredAt.Value <= until.Value);
        }

        query = query.OrderByDescending(c => c.StartedAt);

        var totalCount = await query.CountAsync(ct);
        var canaries = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return Ok(new PagedResultDto<CanaryDto>
        {
            Items = canaries.Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        });
    }

    [HttpGet("summary")]
    public async Task<ActionResult<CanarySummaryDto>> Summary(CancellationToken ct)
    {
        var pendingCount = await db.Canaries.CountAsync(c => c.Status == "Pending", ct);
        var resolvedCount = await db.Canaries.CountAsync(c => c.Status == "Resolved", ct);
        var triggeredCount = await db.Canaries.CountAsync(c => c.Status == "Triggered", ct);

        var now = DateTime.UtcNow;
        var pendingCanaries = await db.Canaries
            .Where(c => c.Status == "Pending")
            .ToListAsync(ct);

        var atRisk = pendingCanaries
            .Where(c => c.Timeout > 0 && (double)(c.Timeout - (c.ExpiresAt - now).TotalSeconds) / c.Timeout >= 0.8)
            .OrderBy(c => c.ExpiresAt)
            .ToList();

        var recent = await db.Canaries
            .Where(c => c.Status == "Resolved" || c.Status == "Triggered")
            .OrderByDescending(c => c.ResolvedAt ?? c.StartedAt)
            .Take(10)
            .ToListAsync(ct);

        return Ok(new CanarySummaryDto
        {
            PendingCount = pendingCount,
            ResolvedCount = resolvedCount,
            TriggeredCount = triggeredCount,
            AtRisk = atRisk.Select(ToDto).ToList(),
            Recent = recent.Select(ToDto).ToList(),
        });
    }

    [HttpPost]
    public async Task<ActionResult<CanaryDto>> Create(CreateCanaryRequest request, CancellationToken ct)
    {
        var canaryType = await db.CanaryTypes.FirstOrDefaultAsync(c => c.Name == request.CanaryType, ct);
        if (canaryType is null)
        {
            return NotFound($"No canary type named '{request.CanaryType}' is configured.");
        }

        var now = DateTime.UtcNow;
        var canary = new Canary
        {
            CanaryType = request.CanaryType,
            ReferenceId = request.ReferenceId,
            Status = "Pending",
            StartedAt = now,
            Timeout = canaryType.Timeout,
            ExpiresAt = now.AddSeconds(canaryType.Timeout),
            Metadata = request.Metadata?.GetRawText(),
        };

        db.Canaries.Add(canary);
        await db.SaveChangesAsync(ct);

        return Ok(ToDto(canary));
    }

    [HttpGet("{canaryType}/{referenceId}")]
    public async Task<ActionResult<CanaryDto>> Get(string canaryType, string referenceId, CancellationToken ct)
    {
        var canary = await db.Canaries.FirstOrDefaultAsync(c => c.CanaryType == canaryType && c.ReferenceId == referenceId, ct);
        if (canary is null) return NotFound();

        return Ok(ToDto(canary));
    }

    [HttpPost("{canaryType}/{referenceId}/resolve")]
    public async Task<ActionResult<CanaryDto>> Resolve(string canaryType, string referenceId, CancellationToken ct)
    {
        var canary = await db.Canaries.FirstOrDefaultAsync(c => c.CanaryType == canaryType && c.ReferenceId == referenceId, ct);
        if (canary is null) return NotFound();

        canary.Status = "Resolved";
        canary.ResolvedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Ok(ToDto(canary));
    }

    [HttpPost("{canaryType}/{referenceId}/pulse")]
    public async Task<ActionResult<CanaryDto>> Pulse(string canaryType, string referenceId, CancellationToken ct)
    {
        var canary = await db.Canaries.FirstOrDefaultAsync(c => c.CanaryType == canaryType && c.ReferenceId == referenceId, ct);
        if (canary is null) return NotFound();

        if (canary.Status != "Pending")
        {
            return Conflict($"Canary '{canaryType}/{referenceId}' is not pending and cannot be pulsed (status: {canary.Status}).");
        }

        canary.ExpiresAt = DateTime.UtcNow.AddSeconds(canary.Timeout);
        await db.SaveChangesAsync(ct);

        return Ok(ToDto(canary));
    }

    [HttpPost("{canaryType}/{referenceId}/checkpoints")]
    public async Task<ActionResult<CanaryCheckpointDto>> CreateCheckpoint(string canaryType, string referenceId, CreateCheckpointRequest request, CancellationToken ct)
    {
        var canary = await db.Canaries.FirstOrDefaultAsync(c => c.CanaryType == canaryType && c.ReferenceId == referenceId, ct);
        if (canary is null) return NotFound();

        var checkpoint = new CanaryCheckpoint
        {
            CanaryId = canary.Id,
            Stage = request.Stage,
            Metadata = request.Metadata?.GetRawText(),
            RecordedAt = DateTime.UtcNow,
        };

        db.CanaryCheckpoints.Add(checkpoint);
        await db.SaveChangesAsync(ct);

        return Ok(ToDto(checkpoint));
    }

    [HttpGet("{canaryType}/{referenceId}/checkpoints")]
    public async Task<ActionResult<List<CanaryCheckpointDto>>> GetCheckpoints(string canaryType, string referenceId, CancellationToken ct)
    {
        var canary = await db.Canaries.FirstOrDefaultAsync(c => c.CanaryType == canaryType && c.ReferenceId == referenceId, ct);
        if (canary is null) return NotFound();

        var checkpoints = await db.CanaryCheckpoints
            .Where(c => c.CanaryId == canary.Id)
            .OrderBy(c => c.RecordedAt)
            .ToListAsync(ct);

        return Ok(checkpoints.Select(ToDto));
    }

    private static CanaryCheckpointDto ToDto(CanaryCheckpoint checkpoint) => new()
    {
        Id = checkpoint.Id,
        CanaryId = checkpoint.CanaryId,
        Stage = checkpoint.Stage,
        Metadata = checkpoint.Metadata is not null ? JsonSerializer.Deserialize<JsonElement>(checkpoint.Metadata) : null,
        RecordedAt = checkpoint.RecordedAt,
    };

    private static CanaryDto ToDto(Canary canary) => new()
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
        Metadata = canary.Metadata is not null ? JsonSerializer.Deserialize<JsonElement>(canary.Metadata) : null,
    };
}
