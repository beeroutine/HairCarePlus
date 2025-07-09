namespace HairCarePlus.Client.Patient.Common.Startup;

using HairCarePlus.Client.Patient.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HairCarePlus.Client.Patient.Features.Sync.Application;
using System.Threading;

/// <summary>
/// Contract for tasks that must execute during application startup before main UI is shown.
/// </summary>
public interface IStartupTask
{
    Task ExecuteAsync();
}

/// <summary>
/// Performs database creation/migration and initial calendar data seeding if required.
/// </summary>
public sealed class DatabaseAndCalendarStartupTask : IStartupTask
{
    private readonly ILocalStorageService _storageService;
    private readonly IDataInitializer _dataInitializer;
    private readonly ILogger<DatabaseAndCalendarStartupTask> _logger;

    public DatabaseAndCalendarStartupTask(ILocalStorageService storageService,
                                          IDataInitializer dataInitializer,
                                          ILogger<DatabaseAndCalendarStartupTask> logger)
    {
        _storageService = storageService;
        _dataInitializer = dataInitializer;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("[Startup] Ensuring database is initialized");
        await _storageService.InitializeDatabaseAsync();
        _logger.LogInformation("[Startup] Database initialization complete");

        if (await _dataInitializer.NeedsInitializationAsync())
        {
            _logger.LogInformation("[Startup] Seeding calendar baseline data");
            await _dataInitializer.InitializeDataAsync();
            _logger.LogInformation("[Startup] Calendar data seeding completed");
        }
    }
}

/// <summary>
/// Performs an initial data synchronization with the server to ensure local cache is populated
/// before the user starts interacting with the UI. This runs only once on app launch.
/// </summary>
public sealed class SyncStartupTask : IStartupTask
{
    private readonly ISyncService _syncService;
    private readonly ILogger<SyncStartupTask> _logger;

    public SyncStartupTask(ISyncService syncService, ILogger<SyncStartupTask> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("[Startup] Performing initial data sync");
        try
        {
            await _syncService.SynchronizeAsync(CancellationToken.None);
            _logger.LogInformation("[Startup] Initial data sync completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Startup] Initial data sync failed – will retry later via scheduler");
        }
    }
}

public static class StartupTaskExtensions
{
    public static IServiceCollection AddStartupTasks(this IServiceCollection services)
    {
        services.AddSingleton<IStartupTask, DatabaseAndCalendarStartupTask>();
        services.AddSingleton<IStartupTask, SyncStartupTask>();
        return services;
    }
} 