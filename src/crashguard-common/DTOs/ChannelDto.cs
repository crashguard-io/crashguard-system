using System.Text.Json;

namespace Crashguard.Common.DTOs;

public class ChannelDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
    public required JsonElement Config { get; set; }
    public DateTime CreatedAt { get; set; }
}
