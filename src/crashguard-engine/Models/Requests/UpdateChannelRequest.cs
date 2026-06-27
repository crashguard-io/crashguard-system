using System.Text.Json;

namespace Crashguard.Engine.Models.Requests;

public class UpdateChannelRequest
{
    public required string Name { get; set; }
    public required string Type { get; set; }
    public required JsonElement Config { get; set; }
}
