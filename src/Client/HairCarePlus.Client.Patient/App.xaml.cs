using Microsoft.Maui.Controls;
using System;
using Microsoft.Extensions.DependencyInjection;
using HairCarePlus.Client.Patient.Common.Startup;
using Microsoft.Extensions.Logging;
using HairCarePlus.Client.Patient.Common.Views;
using System.Linq;
using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient;

public partial class App : Application
{
	private readonly ILogger<App> _logger;
	private Window? _mainWindow;

	public App()
	{
		InitializeComponent();

		// Fallback logger (safe) — will be replaced in OnStart when DI is ready
		_logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<App>();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// Show loading screen while startup tasks execute
		_mainWindow = new Window(new LoadingPage());
		return _mainWindow;
	}

	private async Task RunStartupTasksAsync()
	{
		var logger = IPlatformApplication.Current.Services.GetRequiredService<ILogger<App>>();
		try
		{
			var startupTasks = IPlatformApplication.Current.Services.GetServices<IStartupTask>();
			var tasks = startupTasks.Select(t => t.ExecuteAsync());
			await Task.WhenAll(tasks);
			logger.LogInformation("Startup tasks completed in background.");
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error during background startup tasks execution");
		}
	}

	protected override void OnStart()
	{
		// Keep LoadingPage visible until startup tasks complete to avoid race conditions
		// that could cause the Progress feed to query the database before it is created/migrated.
		_ = Task.Run(async () =>
		{
			await RunStartupTasksAsync();
			await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(() =>
			{
				if (_mainWindow is not null)
				{
					_mainWindow.Page = new AppShell();
				}
			});
		});

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
