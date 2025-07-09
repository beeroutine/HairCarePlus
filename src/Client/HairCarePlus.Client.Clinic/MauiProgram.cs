using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using Microsoft.Maui.Storage;
using SkiaSharp.Views.Maui.Controls.Hosting;
using System.Net.Http;
using HairCarePlus.Client.Clinic.Features.Sync.Application;
using HairCarePlus.Client.Clinic.Features.Sync.Infrastructure;
using System;
using System.Threading.Tasks;
using System.Threading;
using HairCarePlus.Shared.Common;

namespace HairCarePlus.Client.Clinic;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseSkiaSharp()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				// Material Icons — used for various glyphs (e.g., reply arrow)
				fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
			});

		builder.Services.AddSingleton<Infrastructure.Network.Chat.IChatHubConnection, Infrastructure.Network.Chat.SignalRChatHubConnection>();
		builder.Services.AddTransient<Features.Chat.ViewModels.ChatViewModel>();
		builder.Services.AddTransient<Features.Chat.Views.ChatPage>();
		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "clinic.db");
		builder.Services.AddDbContextFactory<Infrastructure.Storage.AppDbContext>(options =>
		    options.UseSqlite($"Data Source={dbPath}"));
		builder.Services.AddDbContext<Infrastructure.Storage.AppDbContext>(options =>
		    options.UseSqlite($"Data Source={dbPath}"), ServiceLifetime.Scoped);
		builder.Services.AddScoped<Features.Chat.Domain.IChatMessageRepository, Infrastructure.Features.Chat.Repositories.ChatMessageRepository>();

		builder.Services.AddTransient<Features.Dashboard.ViewModels.DashboardViewModel>();
		builder.Services.AddTransient<Features.Dashboard.Views.DashboardPage>();

		builder.Services.AddTransient<Features.Patient.Views.PatientPage>();
		builder.Services.AddTransient<Features.Patient.ViewModels.PatientPageViewModel>();
		builder.Services.AddTransient<Features.Patient.Views.PatientProgressPage>();

		builder.UseMauiCommunityToolkit();

		// REST client for API
		var apiBaseUrl = HairCarePlus.Shared.Common.EnvironmentHelper.GetBaseApiUrl();
		builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri($"{apiBaseUrl}/") });
		builder.Services.AddSingleton<CommunityToolkit.Mvvm.Messaging.IMessenger>(CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default);
		builder.Services.AddSingleton<Infrastructure.Network.Events.IEventsSubscription, Infrastructure.Network.Events.SignalREventsSubscription>();
		builder.Services.AddScoped<Infrastructure.Features.Progress.IPhotoReportService, Infrastructure.Features.Progress.PhotoReportService>();
		builder.Services.AddScoped<Infrastructure.Features.Patient.IPatientService, Infrastructure.Features.Patient.PatientService>();
		builder.Services.AddScoped<Infrastructure.Features.Patient.IRestrictionService, Infrastructure.Features.Patient.RestrictionService>();

		// Sync Services
		builder.Services.AddSingleton<IOutboxRepository, OutboxRepository>();
		builder.Services.AddHttpClient<ISyncHttpClient, SyncHttpClient>(client =>
		{
			client.BaseAddress = new Uri($"{HairCarePlus.Shared.Common.EnvironmentHelper.GetBaseApiUrl()}/");
		});

		builder.Services.AddSingleton<ILastSyncVersionStore, PreferencesSyncVersionStore>();
		builder.Services.AddSingleton<ISyncChangeApplier, SyncChangeApplier>();

		builder.Services.AddSingleton<ISyncService, SyncService>();
		builder.Services.AddHostedService<SyncScheduler>();

#if DEBUG
		builder.Logging.ClearProviders();
		builder.Logging.AddConsole();
		builder.Logging.AddDebug();
#endif

#if IOS
		Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("HideAccessory", (handler, view) =>
		{
			if (handler.PlatformView is UIKit.UITextView tv)
			{
				tv.InputAccessoryView = null;
			}
		});
#endif

		// ensure database created
		using (var scope = builder.Services.BuildServiceProvider().CreateScope())
		{
		    var ctx = scope.ServiceProvider.GetRequiredService<Infrastructure.Storage.AppDbContext>();
		    try
		    {
		        ctx.Database.EnsureCreated();

		        // probe for new table; if missing => reset DB (dev-only)
		        try
		        {
		            // simple probe queries to verify that expected tables are present; if any fail we recreate DB (dev-only flow)
		            ctx.Database.ExecuteSqlRaw("SELECT 1 FROM PhotoReports LIMIT 1");
		            ctx.Database.ExecuteSqlRaw("SELECT 1 FROM Restrictions LIMIT 1");
		        }
		        catch (Microsoft.Data.Sqlite.SqliteException)
		        {
		            // One of the new tables is missing — start fresh (no critical user data stored on clinic device yet)
		            ctx.Database.EnsureDeleted();
		            ctx.Database.EnsureCreated();
		        }
		    }
		    catch (Exception ex)
		    {
		        System.Diagnostics.Debug.WriteLine($"DB init failed: {ex.Message}");
		    }
		}

		var app = builder.Build();
		// Fire-and-forget initial sync to populate local cache ASAP (runs in background, non-blocking UI)
		Task.Run(async () =>
		{
		    using var scope = app.Services.CreateScope();
		    var syncSvc = scope.ServiceProvider.GetRequiredService<ISyncService>();
		    try
		    {
		        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
		                                       .CreateLogger("StartupSync");
		        logger.LogInformation("[Startup] Clinic initial sync started");
		        await syncSvc.SynchronizeAsync(CancellationToken.None);
		        logger.LogInformation("[Startup] Clinic initial sync completed");
		    }
		    catch (Exception ex)
		    {
		        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
		                                       .CreateLogger("StartupSync");
		        logger.LogError(ex, "[Startup] Clinic initial sync failed");
		    }
		});

		// Gracefully handle absence of camera on simulators (CommunityToolkit.Maui.Camera throws)

		return app;
	}
}
