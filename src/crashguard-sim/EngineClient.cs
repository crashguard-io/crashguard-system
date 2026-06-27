using Crashguard.Common.DTOs;
using Crashguard.Sim.Models;
using RestSharp;

namespace Crashguard.Sim;

public class EngineClient(RestClient restClient)
{
    public async Task<List<ChannelDto>> GetChannelsAsync(CancellationToken ct)
    {
        var request = new RestRequest("api/channels");
        var response = await restClient.ExecuteGetAsync<List<ChannelDto>>(request, ct);
        response.ThrowIfError();
        return response.Data ?? [];
    }

    public async Task<List<CanaryTypeDto>> GetCanaryTypesAsync(CancellationToken ct)
    {
        var request = new RestRequest("api/canary-types");
        var response = await restClient.ExecuteGetAsync<List<CanaryTypeDto>>(request, ct);
        response.ThrowIfError();
        return response.Data ?? [];
    }

    public async Task<CanaryTypeDto?> CreateCanaryTypeAsync(CreateCanaryTypeRequest createRequest, CancellationToken ct)
    {
        var request = new RestRequest("api/canary-types").AddJsonBody(createRequest);
        var response = await restClient.ExecutePostAsync<CanaryTypeDto>(request, ct);
        response.ThrowIfError();
        return response.Data;
    }

    public async Task<CanaryTypeDto?> UpdateCanaryTypeAsync(int id, UpdateCanaryTypeRequest updateRequest, CancellationToken ct)
    {
        var request = new RestRequest($"api/canary-types/{id}").AddJsonBody(updateRequest);
        var response = await restClient.ExecutePutAsync<CanaryTypeDto>(request, ct);
        response.ThrowIfError();
        return response.Data;
    }
}
