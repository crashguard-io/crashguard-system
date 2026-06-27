namespace Crashguard.Common.DTOs;

public class CanaryTypeStatusDto
{
    public required string CanaryType { get; set; }
    public int PendingCount { get; set; }
    public int AtRiskCount { get; set; }
    public int TriggeredSinceCount { get; set; }
}
