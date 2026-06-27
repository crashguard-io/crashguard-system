namespace Crashguard.Engine.Models;

public class CanaryTypeRule
{
    public int Id { get; set; }
    public int CanaryTypeId { get; set; }
    public required string Field { get; set; }
    public required string Operator { get; set; }
    public string? Value { get; set; }
    public required string Severity { get; set; }
    public required string Channel { get; set; }
}
