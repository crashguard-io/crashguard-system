namespace Crashguard.Common.DTOs;

public class CanaryTypeDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int Timeout { get; set; }
    public int ExtendLimit { get; set; }
    public int DedupInterval { get; set; }
    public int RenotifyInterval { get; set; }
    public required string Severity { get; set; }
    public string? MetadataSchema { get; set; }
    public string? VerifierUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public required List<CanaryTypeRuleDto> Rules { get; set; }
    public required List<int> DefaultChannelIds { get; set; }
}
