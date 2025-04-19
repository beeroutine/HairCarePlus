using Microsoft.Maui.Controls;
using System;
using Microsoft.Extensions.DependencyInjection;
using HairCarePlus.Client.Patient.Infrastructure.Storage;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Client.Patient;

public partial class App : Application
{
	private readonly ILocalStorageService _storageService;
	private readonly IDataInitializer _dataInitializer;
	private readonly ILogger<App> _logger;
	private bool _isDatabaseInitialized;

	public App()
	{
		try
		{
			_logger = IPlatformApplication.Current.Services.GetRequiredService<ILogger<App>>();
			_logger.LogDebug("App constructor start");
			InitializeComponent();
			_logger.LogDebug("App InitializeComponent completed");
			
			MainPage = new AppShell();
			_logger.LogDebug("AppShell initialized and set as MainPage");
			
			_storageService = IPlatformApplication.Current.Services.GetRequiredService<ILocalStorageService>();
			_logger.LogDebug("LocalStorageService retrieved");
			
			_dataInitializer = IPlatformApplication.Current.Services.GetRequiredService<IDataInitializer>();
			_logger.LogDebug("DataInitializer retrieved");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Exception in App constructor");
			// Consider showing an error message to the user here
		}
	}

	protected override async void OnStart()
	{
		_logger.LogInformation("App.OnStart called");
		try
		{
			if (!_isDatabaseInitialized)
			{
				_logger.LogInformation("Starting database initialization");
				await _storageService.InitializeDatabaseAsync();
				_isDatabaseInitialized = true;
				_logger.LogInformation("Database initialization completed successfully");
				
				// Проверяем необходимость инициализации данных календаря
				if (await _dataInitializer.NeedsInitializationAsync())
				{
					_logger.LogInformation("Starting calendar data initialization");
					await _dataInitializer.InitializeDataAsync();
					_logger.LogInformation("Calendar data initialization completed successfully");
				}
				else
				{
					_logger.LogInformation("Calendar data already initialized, skipping initialization");
				}
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error initializing database");
			// Consider showing an error message to the user here
		}
		base.OnStart();
	}

	protected override void OnSleep()
	{
		_logger.LogDebug("App.OnSleep called");
		base.OnSleep();
	}

	protected override void OnResume()
	{
		_logger.LogDebug("App.OnResume called");
		base.OnResume();
	}
}
