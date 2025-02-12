using System.Text.Json;

namespace HairCarePlus.Client.Patient.Infrastructure.Services;

public class LocalStorageService : ILocalStorageService
{
    private readonly JsonSerializerOptions _jsonOptions;

    public LocalStorageService()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<T> GetAsync<T>(string key)
    {
        var json = await SecureStorage.GetAsync(key);
        if (string.IsNullOrEmpty(json))
        {
            return default!;
        }
        return JsonSerializer.Deserialize<T>(json)!;
    }

    public async Task SetAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        await SecureStorage.SetAsync(key, json);
    }

    public async Task RemoveAsync(string key)
    {
        SecureStorage.Remove(key);
        await Task.CompletedTask;
    }

    public async Task ClearAsync()
    {
        SecureStorage.RemoveAll();
        await Task.CompletedTask;
    }

    public bool Contains(string key)
    {
        var result = SecureStorage.Default.GetAsync(key).Result;
        return !string.IsNullOrEmpty(result);
    }
} 