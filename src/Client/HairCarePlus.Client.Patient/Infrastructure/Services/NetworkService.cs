using System.Net.Http.Json;
using System.Text.Json;

namespace HairCarePlus.Client.Patient.Infrastructure.Services;

public class NetworkService : INetworkService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public NetworkService()
    {
        _httpClient = new HttpClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<T> GetAsync<T>(string endpoint, IDictionary<string, string>? headers = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        AddHeaders(request, headers);
        
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions) ?? throw new Exception($"Failed to deserialize response to type {typeof(T).Name}");
    }

    public async Task<T> PostAsync<T>(string endpoint, object data, IDictionary<string, string>? headers = null)
    {
        var response = await _httpClient.PostAsJsonAsync(endpoint, data);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions) ?? throw new Exception($"Failed to deserialize response to type {typeof(T).Name}");
    }

    public async Task<T> PutAsync<T>(string endpoint, object data, IDictionary<string, string>? headers = null)
    {
        var response = await _httpClient.PutAsJsonAsync(endpoint, data);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions) ?? throw new Exception($"Failed to deserialize response to type {typeof(T).Name}");
    }

    public async Task<T> DeleteAsync<T>(string endpoint, IDictionary<string, string>? headers = null)
    {
        var response = await _httpClient.DeleteAsync(endpoint);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions) ?? throw new Exception($"Failed to deserialize response to type {typeof(T).Name}");
    }

    public Task<bool> IsConnectedAsync()
    {
        return Task.FromResult(Connectivity.NetworkAccess == NetworkAccess.Internet);
    }

    public async Task<HttpResponseMessage> UploadFileAsync(string endpoint, Stream fileStream, string fileName, string contentType, IDictionary<string, string>? headers = null)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);

        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
        return response;
    }

    public void SetAuthToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    private void AddHeaders(HttpRequestMessage request, IDictionary<string, string>? headers)
    {
        if (headers != null)
        {
            foreach (var header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }
    }
} 