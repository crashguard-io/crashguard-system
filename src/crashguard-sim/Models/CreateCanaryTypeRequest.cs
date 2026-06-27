namespace Crashguard.Sim.Models;

public class CreateCanaryTypeRequest
{
    public required string Name { get; set; }
    public int Timeout { get; set; }
    public int ExtendLimit { get; set; }
    public int DedupInterval { get; set; }
    public int RenotifyInterval { get; set; }
    public required string Severity { get; set; }
    public string? VerifierUrl { get; set; }
    public List<int> DefaultChannelIds { get; set; } = [];
}
