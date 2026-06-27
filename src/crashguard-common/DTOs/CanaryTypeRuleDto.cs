namespace Crashguard.Common.DTOs;

public class CanaryTypeRuleDto
{
    public int Id { get; set; }
    public required string Field { get; set; }
    public required string Operator { get; set; }
    public string? Value { get; set; }
    public required string Severity { get; set; }
    public required string Channel { get; set; }
}
