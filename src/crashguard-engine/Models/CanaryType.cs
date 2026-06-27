namespace Crashguard.Engine.Models;

public class CanaryType
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int Timeout { get; set; }
    public int ExtendLimit { get; set; }

    /// <summary>
    /// Quiet period (seconds) of no new triggers for this type before an open alert batch is closed.
    /// Each new trigger slides this window forward. Zero disables deduplication entirely.
    /// </summary>
    public int DedupInterval { get; set; }

    /// <summary>
    /// While a batch stays open past this many seconds (seconds), send a "still firing" summary and
    /// reset the clock. Zero disables renotification (the batch stays silent until it closes).
    /// </summary>
    public int RenotifyInterval { get; set; }

    public required string Severity { get; set; }
    public string? MetadataSchema { get; set; }
    public string? VerifierUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<CanaryTypeRule> Rules { get; set; } = [];
    public List<CanaryTypeChannel> DefaultChannels { get; set; } = [];
}
