using Microsoft.Maui.Controls;
using System;
using Microsoft.Extensions.DependencyInjection;
using HairCarePlus.Client.Patient.Common.Startup;
using Microsoft.Extensions.Logging;
using HairCarePlus.Client.Patient.Common.Views;

namespace HairCarePlus.Client.Patient;

public partial class App : Application
{
	private readonly ILogger<App> _logger;

	public App()
	{
		try
		{
			_logger = IPlatformApplication.Current.Services.GetRequiredService<ILogger<App>>();
			_logger.LogDebug("App constructor start");
			InitializeComponent();
			_logger.LogDebug("App InitializeComponent completed");
			
			// Show loading screen while startup tasks execute
			MainPage = new LoadingPage();
			_logger.LogDebug("LoadingPage set as MainPage");
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
			var startupTasks = IPlatformApplication.Current.Services.GetServices<IStartupTask>();
			foreach (var task in startupTasks)
			{
				await task.ExecuteAsync();
			}

			MainPage = new AppShell();
			_logger.LogInformation("Startup tasks completed. AppShell displayed.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error during startup tasks execution");
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
