namespace Crashguard.Engine.Models;

/// <summary>
/// Tracks an open deduplication window for a canary type: while open, new triggers of that type
/// are folded into this batch instead of sending a separate alert. See Service.TriggerAsync and
/// Service.SweepAlertBatchesAsync for the open/renotify/close lifecycle.
/// </summary>
public class CanaryAlertBatch
{
    public int Id { get; set; }
    public int CanaryTypeId { get; set; }

    /// <summary>
    /// The resolved destination for this batch: a specific channel's name if a rule matched, or
    /// <see cref="Services.Service.DefaultDestinationChannel"/> if none did and the canary type's
    /// default channels/severity were used instead. Combined with <see cref="Severity"/>, this is
    /// the batch key — canaries of the same type that resolve to different destinations or
    /// severities are never folded into the same batch.
    /// </summary>
    public required string Channel { get; set; }

    /// <summary>The effective severity for this batch — either a rule's override or the canary type's default.</summary>
    public required string Severity { get; set; }

    public DateTime OpenedAt { get; set; }

    /// <summary>
    /// Timestamp of the most recent trigger folded into this batch. Each new trigger pushes this
    /// forward, which is what makes the dedup window slide rather than being a fixed bucket.
    /// </summary>
    public DateTime LastFailureAt { get; set; }

    public DateTime LastNotifiedAt { get; set; }

    /// <summary>Total triggers folded into this batch since it opened.</summary>
    public int Count { get; set; }

    /// <summary>Triggers folded in since the last notification (initial send or renotify).</summary>
    public int CountSinceLastNotify { get; set; }

    public int LastCanaryId { get; set; }

    public bool IsOpen { get; set; }
}
