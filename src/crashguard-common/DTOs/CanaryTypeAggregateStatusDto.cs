namespace Crashguard.Common.DTOs;

public class CanaryTypeAggregateStatusDto
{
    public int PendingCount { get; set; }
    public int ResolvedCount { get; set; }
    public int AtRiskCount { get; set; }
    public int TriggeredCount { get; set; }
}
