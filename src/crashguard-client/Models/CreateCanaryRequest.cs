namespace Crashguard.Client.Models;

public class CreateCanaryRequest
{
    public required string CanaryType { get; set; }
    public required string ReferenceId { get; set; }
    public object? Metadata { get; set; }
}
