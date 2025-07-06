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

		builder.UseMauiCommunityToolkit();

		// REST client for API
		var apiBaseUrl = Environment.GetEnvironmentVariable("CHAT_BASE_URL") ?? "http://127.0.0.1:5281/";
		builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
		builder.Services.AddSingleton<CommunityToolkit.Mvvm.Messaging.IMessenger>(CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default);
		builder.Services.AddSingleton<Infrastructure.Network.Events.IEventsSubscription, Infrastructure.Network.Events.SignalREventsSubscription>();
		builder.Services.AddScoped<Infrastructure.Features.Progress.IPhotoReportService, Infrastructure.Features.Progress.PhotoReportService>();
		builder.Services.AddScoped<Infrastructure.Features.Patient.IPatientService, Infrastructure.Features.Patient.PatientService>();
		builder.Services.AddScoped<Infrastructure.Features.Patient.IRestrictionService, Infrastructure.Features.Patient.RestrictionService>();

		// Sync Services
		builder.Services.AddSingleton<IOutboxRepository, OutboxRepository>();
		builder.Services.AddHttpClient<ISyncHttpClient, SyncHttpClient>(client =>
		{
			var baseUrl = Environment.GetEnvironmentVariable("CHAT_BASE_URL") ?? "http://10.153.34.67:5281/";
			client.BaseAddress = new Uri(baseUrl);
		});
		builder.Services.AddSingleton<ISyncService, SyncService>();
		builder.Services.AddHostedService<SyncScheduler>();

#if DEBUG
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
		            ctx.Database.ExecuteSqlRaw("SELECT 1 FROM PhotoReports LIMIT 1");
		        }
		        catch (Microsoft.Data.Sqlite.SqliteException)
		        {
		            // table absent ⇒ drop and recreate (no user data yet)
		            ctx.Database.EnsureDeleted();
		            ctx.Database.EnsureCreated();
		        }
		    }
		    catch (Exception ex)
		    {
		        System.Diagnostics.Debug.WriteLine($"DB init failed: {ex.Message}");
		    }
		}

		return builder.Build();
	}
}
