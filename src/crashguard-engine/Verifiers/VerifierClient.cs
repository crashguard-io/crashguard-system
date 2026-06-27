using System.Text.Json;
using System.Text.Json.Serialization;
using Crashguard.Engine.Models;
using RestSharp;

namespace Crashguard.Engine.Verifiers;

/// <summary>
/// Calls the external verifier configured on a <see cref="CanaryType"/> (<see cref="CanaryType.VerifierUrl"/>)
/// for an overdue canary, and parses the action it wants the engine to take.
/// </summary>
public class VerifierClient(RestClient restClient) : IVerifierClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task<VerifierResponse> VerifyAsync(string verifierUrl, Canary canary, CancellationToken ct)
    {
        var payload = new
        {
            canaryType = canary.CanaryType,
            referenceId = canary.ReferenceId,
            status = canary.Status,
            startedAt = canary.StartedAt,
            expiresAt = canary.ExpiresAt,
            extendCount = canary.ExtendCount,
            metadata = canary.Metadata is not null ? JsonSerializer.Deserialize<JsonElement>(canary.Metadata) : (JsonElement?)null,
        };

        var request = new RestRequest(verifierUrl).AddJsonBody(payload);
        var response = await restClient.ExecutePostAsync(request, ct);
        response.ThrowIfError();

        if (string.IsNullOrWhiteSpace(response.Content))
        {
            throw new InvalidOperationException($"Verifier at '{verifierUrl}' returned an empty response.");
        }

        var result = JsonSerializer.Deserialize<VerifierResponse>(response.Content, JsonOptions);
        if (result is null)
        {
            throw new InvalidOperationException($"Verifier at '{verifierUrl}' returned an unparseable response.");
        }

        return result;
    }
}
