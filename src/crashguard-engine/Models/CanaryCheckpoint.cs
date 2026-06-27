namespace Crashguard.Engine.Models;

public class CanaryCheckpoint
{
    public int Id { get; set; }
    public int CanaryId { get; set; }
    public required string Stage { get; set; }
    public string? Metadata { get; set; }
    public DateTime RecordedAt { get; set; }
}
