using System.Text.Json;

namespace Crashguard.Common.DTOs;

public class CanaryDto
{
    public int Id { get; set; }
    public required string CanaryType { get; set; }
    public required string ReferenceId { get; set; }
    public required string Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? TriggeredAt { get; set; }
    public int Timeout { get; set; }
    public DateTime ExpiresAt { get; set; }
    public JsonElement? Metadata { get; set; }
}
