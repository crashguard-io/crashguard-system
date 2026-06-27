namespace Crashguard.Common.DTOs;

public class CanarySummaryDto
{
    public int PendingCount { get; set; }
    public int ResolvedCount { get; set; }
    public int TriggeredCount { get; set; }
    public required List<CanaryDto> AtRisk { get; set; }
    public required List<CanaryDto> Recent { get; set; }
}
