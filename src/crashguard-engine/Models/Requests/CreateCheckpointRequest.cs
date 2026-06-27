using System.Text.Json;

namespace Crashguard.Engine.Models.Requests;

public class CreateCheckpointRequest
{
    public required string Stage { get; set; }
    public JsonElement? Metadata { get; set; }
}
