namespace Crashguard.Client.Models;

public class ResolveCanaryRequest
{
    public required string CanaryType { get; set; }
    public required string ReferenceId { get; set; }
}
