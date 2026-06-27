namespace Crashguard.Engine.Models;

public class Channel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
    public required string Config { get; set; }
    public DateTime CreatedAt { get; set; }
}
