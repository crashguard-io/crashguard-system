namespace Crashguard.Sim.Models;

public class LoadTestOptions
{
    public required int CanaryCount { get; set; }
    public required int ApiPort { get; set; }
    public required int VerifyDelayMs { get; set; }
}
