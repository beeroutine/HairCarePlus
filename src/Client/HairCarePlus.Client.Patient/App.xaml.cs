using Microsoft.Maui.Controls;
using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using HairCarePlus.Client.Patient.Infrastructure.Storage;

namespace HairCarePlus.Client.Patient;

public partial class App : Application
{
	private readonly ILocalStorageService _storageService;
	private readonly IDataInitializer _dataInitializer;
	private bool _isDatabaseInitialized;

	public App()
	{
		try
		{
			Debug.WriteLine("App constructor start");
			InitializeComponent();
			Debug.WriteLine("App InitializeComponent completed");
			
			MainPage = new AppShell();
			Debug.WriteLine("AppShell initialized and set as MainPage");
			
			_storageService = IPlatformApplication.Current.Services.GetRequiredService<ILocalStorageService>();
			Debug.WriteLine("LocalStorageService retrieved");
			
			_dataInitializer = IPlatformApplication.Current.Services.GetRequiredService<IDataInitializer>();
			Debug.WriteLine("DataInitializer retrieved");
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Exception in App constructor: {ex.Message}");
			Debug.WriteLine($"Stack trace: {ex.StackTrace}");
		}
	}

	protected override async void OnStart()
	{
		Debug.WriteLine("App.OnStart called");
		try
		{
			if (!_isDatabaseInitialized)
			{
				Debug.WriteLine("Starting database initialization");
				await _storageService.InitializeDatabaseAsync();
				_isDatabaseInitialized = true;
				Debug.WriteLine("Database initialization completed successfully");
				
				// Проверяем необходимость инициализации данных календаря
				if (await _dataInitializer.NeedsInitializationAsync())
				{
					Debug.WriteLine("Starting calendar data initialization");
					await _dataInitializer.InitializeDataAsync();
					Debug.WriteLine("Calendar data initialization completed successfully");
				}
				else
				{
					Debug.WriteLine("Calendar data already initialized, skipping initialization");
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error initializing database: {ex.Message}");
			Debug.WriteLine($"Stack trace: {ex.StackTrace}");
			// Consider showing an error message to the user here
		}
		base.OnStart();
	}

	protected override void OnSleep()
	{
		Debug.WriteLine("App.OnSleep called");
		base.OnSleep();
	}

	protected override void OnResume()
	{
		Debug.WriteLine("App.OnResume called");
		base.OnResume();
	}
}
