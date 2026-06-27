using System.Text.Json;

namespace Crashguard.Common.DTOs;

public class CanaryCheckpointDto
{
    public int Id { get; set; }
    public int CanaryId { get; set; }
    public required string Stage { get; set; }
    public JsonElement? Metadata { get; set; }
    public DateTime RecordedAt { get; set; }
}
