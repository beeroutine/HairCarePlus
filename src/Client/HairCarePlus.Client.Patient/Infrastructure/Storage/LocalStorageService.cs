using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;
using System.Threading;

namespace HairCarePlus.Client.Patient.Infrastructure.Storage;

public class LocalStorageService : ILocalStorageService
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _databasePath;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private const int MAX_RETRIES = 3;
    private const int RETRY_DELAY_MS = 1000;
    private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);
    private readonly ILogger<LocalStorageService>? _logger;

    public LocalStorageService(IDbContextFactory<AppDbContext> contextFactory, ILogger<LocalStorageService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "haircare.db");
        _databasePath = dbPath;
    }

    public AppDbContext GetDbContext()
    {
        return _contextFactory.CreateDbContext();
    }

    public async Task InitializeDatabaseAsync()
    {
        if (!await _initLock.WaitAsync(TimeSpan.FromSeconds(30)))
        {
            throw new TimeoutException("Could not acquire initialization lock");
        }

        try
        {
            using var context = GetDbContext();

            // Ensure base schema exists (creates DB if missing)
            await context.Database.EnsureCreatedAsync();

            _logger?.LogDebug("Database exists, verifying mandatory tables");

            // DEV-time safety: verify critical tables; if any probe fails we recreate DB (no irreversible data yet)
            bool rebuildRequired = false;
            try
            {
                context.Database.ExecuteSqlRaw("SELECT 1 FROM OutboxItems LIMIT 1");
                context.Database.ExecuteSqlRaw("SELECT 1 FROM PhotoReports LIMIT 1");
                }
            catch (Microsoft.Data.Sqlite.SqliteException ex)
                {
                _logger?.LogWarning(ex, "Required tables missing – will rebuild SQLite file");
                    rebuildRequired = true;
            }

            if (rebuildRequired)
            {
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();
                _logger?.LogInformation("Local SQLite database recreated with latest schema");
            }
        }
        finally
        {
            _initLock.Release();
        }
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
        long size = 0;
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
        await context.Database.EnsureCreatedAsync();
        _logger?.LogDebug("Database cleared and recreated");
    }

    public string GetDatabasePath() => _databasePath;

    private async Task<bool> DoesDatabaseExistAsync()
    {
        if (!File.Exists(_databasePath))
        {
            return false;
        }

        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
} 