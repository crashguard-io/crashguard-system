namespace Crashguard.Common.DTOs;

public class CanaryTypeHistoryBucketDto
{
    public required DateTime BucketStart { get; set; }
    public int TriggeredCount { get; set; }
    public int ResolvedCount { get; set; }
    public int PendingCount { get; set; }
    public double? AvgResolutionSeconds { get; set; }
}

public class CanaryTypeHistoryDto
{
    public required string CanaryType { get; set; }
    public List<CanaryTypeHistoryBucketDto> Buckets { get; set; } = [];
}
