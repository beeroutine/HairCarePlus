using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Shared.Communication.Sync;

namespace HairCarePlus.Client.Patient.Features.Sync.Infrastructure;

public interface ISyncHttpClient
{
    Task<BatchSyncResponseDto?> PushAsync(BatchSyncRequestDto request, CancellationToken ct);
}

public class SyncHttpClient : ISyncHttpClient
{
    private readonly HttpClient _httpClient;

    public SyncHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<BatchSyncResponseDto?> PushAsync(BatchSyncRequestDto request, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("sync/batch", request, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<BatchSyncResponseDto>(cancellationToken: ct);
        }
        catch (HttpRequestException)
        {
            // log error
            return null;
        }
    }
} 