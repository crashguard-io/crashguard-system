using System.Text.Json;

namespace Crashguard.Engine.Models.Requests;

public class CreateCanaryRequest
{
    public required string CanaryType { get; set; }
    public required string ReferenceId { get; set; }
    public JsonElement? Metadata { get; set; }
}
