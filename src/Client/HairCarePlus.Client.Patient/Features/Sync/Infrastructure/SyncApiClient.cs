using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HairCarePlus.Shared.Communication.Sync;

namespace HairCarePlus.Client.Patient.Features.Sync.Infrastructure;

public interface ISyncApiClient
{
    Task<BatchSyncResponseDto?> SendBatchAsync(BatchSyncRequestDto request);
}

public sealed class SyncApiClient : ISyncApiClient
{
    private readonly HttpClient _http;

    public SyncApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<BatchSyncResponseDto?> SendBatchAsync(BatchSyncRequestDto request)
    {
        var response = await _http.PostAsJsonAsync("sync/batch", request);
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.Content.ReadFromJsonAsync<BatchSyncResponseDto>();
    }
} 