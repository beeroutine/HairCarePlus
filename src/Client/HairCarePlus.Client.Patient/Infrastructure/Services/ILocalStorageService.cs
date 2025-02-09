namespace HairCarePlus.Client.Patient.Infrastructure.Services
{
    public interface ILocalStorageService
    {
        Task<T> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value);
        Task RemoveAsync(string key);
        Task ClearAsync();
        bool Contains(string key);
    }
} 