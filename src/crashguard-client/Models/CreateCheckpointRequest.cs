using System.Text.Json;

namespace Crashguard.Client.Models;

public class CreateCheckpointRequest
{
    public required string Stage { get; set; }
    public JsonElement? Metadata { get; set; }
}
