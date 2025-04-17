using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
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

    public LocalStorageService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
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
            if (!await DoesDatabaseExistAsync())
            {
                Debug.WriteLine("Database does not exist, creating...");
                await CreateDatabaseWithSqliteAsync();
                Debug.WriteLine("Database created successfully");
            }
            else
            {
                Debug.WriteLine("Database exists, verifying schema...");
                if (!await VerifyDatabaseTablesAsync())
                {
                    Debug.WriteLine("Database schema verification failed, recreating...");
                    await CreateDatabaseWithSqliteAsync();
                    Debug.WriteLine("Database recreated successfully");
                }
                else
                {
                    Debug.WriteLine("Database schema verified, skipping initialization");
                }
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task<bool> VerifyDatabaseTablesExistAsync()
    {
        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();
            
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT name FROM sqlite_master 
                WHERE type='table' 
                AND name IN ('Events', 'ChatMessages')";
            
            using var reader = await command.ExecuteReaderAsync();
            var tableCount = 0;
            while (await reader.ReadAsync())
            {
                tableCount++;
                Debug.WriteLine($"Found table: {reader.GetString(0)}");
            }
            
            return tableCount == 2; // We need both Events and ChatMessages tables
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error verifying database tables: {ex.Message}");
            return false;
        }
    }

    private async Task CreateDatabaseWithSqliteAsync()
    {
        try
        {
            Debug.WriteLine("Creating database tables using direct SQL commands");
            
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();
            
            // Create Events table
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Events (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    Date TEXT NOT NULL,
                    StartDate TEXT NOT NULL,
                    EndDate TEXT,
                    CreatedAt TEXT NOT NULL,
                    ModifiedAt TEXT NOT NULL,
                    IsCompleted INTEGER NOT NULL,
                    EventType INTEGER NOT NULL,
                    Priority INTEGER NOT NULL,
                    TimeOfDay INTEGER NOT NULL,
                    ReminderTime TEXT NOT NULL,
                    ExpirationDate TEXT
                )";
                await command.ExecuteNonQueryAsync();
                Debug.WriteLine("Created Events table");
            }
            
            // Create ChatMessages table (not Messages)
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                CREATE TABLE IF NOT EXISTS ChatMessages (
                    LocalId INTEGER PRIMARY KEY AUTOINCREMENT,
                    ServerMessageId TEXT,
                    ConversationId TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    SentAt TEXT NOT NULL,
                    Timestamp TEXT,
                    SenderId TEXT NOT NULL,
                    RecipientId TEXT,
                    Type INTEGER,
                    Status INTEGER,
                    SyncStatus INTEGER,
                    IsRead INTEGER,
                    AttachmentUrl TEXT,
                    LocalAttachmentPath TEXT,
                    ThumbnailUrl TEXT,
                    LocalThumbnailPath TEXT,
                    FileSize INTEGER,
                    FileName TEXT,
                    MimeType TEXT,
                    ReadAt TEXT,
                    DeliveredAt TEXT,
                    ReplyToLocalId INTEGER,
                    CreatedAt TEXT,
                    LastModifiedAt TEXT
                )";
                await command.ExecuteNonQueryAsync();
                Debug.WriteLine("Created ChatMessages table");
            }
            
            Debug.WriteLine("Database schema created successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating database with SQLite: {ex.Message}");
            throw;
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
        Debug.WriteLine("Database cleared and recreated");
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

    private async Task<bool> VerifyDatabaseTablesAsync()
    {
        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();
            
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT name FROM sqlite_master 
                WHERE type='table' 
                AND name IN ('Events', 'ChatMessages')";
            
            using var reader = await command.ExecuteReaderAsync();
            var tableCount = 0;
            while (await reader.ReadAsync())
            {
                tableCount++;
                Debug.WriteLine($"Found table: {reader.GetString(0)}");
            }
            
            return tableCount == 2; // We need both Events and ChatMessages tables
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error verifying database tables: {ex.Message}");
            return false;
        }
    }
} 