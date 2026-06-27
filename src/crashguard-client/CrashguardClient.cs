using System.Text.Json;
using Crashguard.Client.Models;
using Crashguard.Common.DTOs;
using RestSharp;

namespace Crashguard.Client;

public class CrashguardClient
{
    private readonly RestClient _restClient;

    public CrashguardClient(RestClient restClient)
    {
        _restClient = restClient;
    }

    public async Task<CanaryDto?> CreateCanaryAsync(CreateCanaryRequest request, CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest("api/canaries").AddJsonBody(request);
        var response = await _restClient.ExecutePostAsync<CanaryDto>(restRequest, cancellationToken);
        response.ThrowIfError();
        return response.Data;
    }

    public async Task<CanaryDto?> GetCanaryAsync(string canaryType, string referenceId, CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest($"api/canaries/{canaryType}/{referenceId}");
        var response = await _restClient.ExecuteGetAsync<CanaryDto>(restRequest, cancellationToken);
        response.ThrowIfError();
        return response.Data;
    }

    public async Task<CanaryDto?> ResolveCanaryAsync(string canaryType, string referenceId, CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest($"api/canaries/{canaryType}/{referenceId}/resolve")
            .AddJsonBody(new ResolveCanaryRequest { CanaryType = canaryType, ReferenceId = referenceId });
        var response = await _restClient.ExecutePostAsync<CanaryDto>(restRequest, cancellationToken);
        response.ThrowIfError();
        return response.Data;
    }

    public async Task<CanaryDto?> PulseCanaryAsync(string canaryType, string referenceId, CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest($"api/canaries/{canaryType}/{referenceId}/pulse");
        var response = await _restClient.ExecutePostAsync<CanaryDto>(restRequest, cancellationToken);
        response.ThrowIfError();
        return response.Data;
    }

    public async Task<CanaryCheckpointDto?> CheckpointCanaryAsync(string canaryType, string referenceId, string stage, object? metadata = null, CancellationToken cancellationToken = default)
    {
        var request = new CreateCheckpointRequest
        {
            Stage = stage,
            Metadata = metadata is null ? null : JsonSerializer.SerializeToElement(metadata),
        };
        var restRequest = new RestRequest($"api/canaries/{canaryType}/{referenceId}/checkpoints").AddJsonBody(request);
        var response = await _restClient.ExecutePostAsync<CanaryCheckpointDto>(restRequest, cancellationToken);
        response.ThrowIfError();
        return response.Data;
    }

    public async Task<List<CanaryCheckpointDto>?> GetCheckpointsAsync(string canaryType, string referenceId, CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest($"api/canaries/{canaryType}/{referenceId}/checkpoints");
        var response = await _restClient.ExecuteGetAsync<List<CanaryCheckpointDto>>(restRequest, cancellationToken);
        response.ThrowIfError();
        return response.Data;
    }
}
