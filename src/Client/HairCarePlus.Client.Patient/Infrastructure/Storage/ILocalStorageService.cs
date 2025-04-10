using System;
using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Infrastructure.Storage;

public interface ILocalStorageService
{
    Task InitializeDatabaseAsync();
    AppDbContext GetDbContext();
    Task ClearDatabaseAsync();
    string GetDatabasePath();
    Task<T> GetItemAsync<T>(string key) where T : class;
    Task SetItemAsync<T>(string key, T value) where T : class;
    Task RemoveItemAsync(string key);
    Task ClearAsync();
    Task<bool> ContainsKeyAsync(string key);
    Task<DateTime> GetLastSyncTimeAsync(string key);
    Task SetLastSyncTimeAsync(string key, DateTime time);
    Task<long> GetStorageSizeAsync();
    Task<bool> IsStorageAvailableAsync();
} 