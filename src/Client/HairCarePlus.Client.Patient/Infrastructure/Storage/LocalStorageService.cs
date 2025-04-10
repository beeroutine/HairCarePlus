using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace HairCarePlus.Client.Patient.Infrastructure.Storage;

public class LocalStorageService : ILocalStorageService
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _databasePath;
    private AppDbContext? _dbContext;

    public LocalStorageService()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "haircare.db");
        _databasePath = dbPath;
    }

    public AppDbContext GetDbContext()
    {
        if (_dbContext == null)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={_databasePath}")
                .Options;
            _dbContext = new AppDbContext(options);
        }
        return _dbContext;
    }

    public async Task InitializeDatabaseAsync()
    {
        var context = GetDbContext();
        await context.Database.MigrateAsync();
    }

    public async Task<T> GetItemAsync<T>(string key) where T : class
    {
        var json = await SecureStorage.GetAsync(key);
        if (string.IsNullOrEmpty(json))
        {
            return default!;
        }
        return JsonSerializer.Deserialize<T>(json, _jsonOptions)!;
    }

    public async Task SetItemAsync<T>(string key, T value) where T : class
    {
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        await SecureStorage.SetAsync(key, json);
    }

    public async Task RemoveItemAsync(string key)
    {
        SecureStorage.Remove(key);
        await Task.CompletedTask;
    }

    public async Task ClearAsync()
    {
        SecureStorage.RemoveAll();
        await Task.CompletedTask;
    }

    public async Task<bool> ContainsKeyAsync(string key)
    {
        var result = await SecureStorage.GetAsync(key);
        return !string.IsNullOrEmpty(result);
    }

    public async Task<DateTime> GetLastSyncTimeAsync(string key)
    {
        var timestamp = await SecureStorage.GetAsync($"{key}_sync_time");
        if (DateTime.TryParse(timestamp, out var result))
        {
            return result;
        }
        return DateTime.MinValue;
    }

    public async Task SetLastSyncTimeAsync(string key, DateTime time)
    {
        await SecureStorage.SetAsync($"{key}_sync_time", time.ToString("O"));
    }

    public async Task<long> GetStorageSizeAsync()
    {
        // This is a rough estimation as SecureStorage doesn't provide direct size information
        long size = 0;
        // Implementation details would depend on the specific storage mechanism
        await Task.CompletedTask;
        return size;
    }

    public async Task<bool> IsStorageAvailableAsync()
    {
        try
        {
            const string testKey = "_storage_test";
            await SecureStorage.SetAsync(testKey, "test");
            await SecureStorage.GetAsync(testKey);
            await RemoveItemAsync(testKey);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task ClearDatabaseAsync()
    {
        var context = GetDbContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    public string GetDatabasePath() => _databasePath;
} 