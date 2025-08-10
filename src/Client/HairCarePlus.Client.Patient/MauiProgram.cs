using HairCarePlus.Client.Patient.Features.Chat;
using HairCarePlus.Client.Patient.Features.Chat.ViewModels;
using HairCarePlus.Client.Patient.Features.Chat.Views;
using HairCarePlus.Client.Patient.Infrastructure.Services;
using HairCarePlus.Client.Patient.Infrastructure.Storage;
using HairCarePlus.Client.Patient.Infrastructure.Storage.Repositories;
using HairCarePlus.Client.Patient.Infrastructure.Security;
using HairCarePlus.Client.Patient.Infrastructure.Media;
using HairCarePlus.Client.Patient.Common.Behaviors;
using HairCarePlus.Client.Patient.Features.Calendar;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using HairCarePlus.Client.Patient.Features.Calendar.Views;
using System.Net.Http;
using HairCarePlus.Client.Patient.Features.Calendar.Helpers;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Implementation;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using HairCarePlus.Client.Patient.Features.Notifications.Services.Interfaces;
using System;
using System.IO;
using System.Diagnostics;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using HairCarePlus.Client.Patient.Features.Calendar.Converters;
using HairCarePlus.Client.Patient.Common.Startup;
using CommunityToolkit.Mvvm.Messaging;
using HairCarePlus.Client.Patient.Features.PhotoCapture;
using HairCarePlus.Client.Patient.Features.Progress;
using Microsoft.Maui.Handlers;
using SkiaSharp.Views.Maui.Controls.Hosting;
using HairCarePlus.Client.Patient.Features.Sync;
using HairCarePlus.Client.Patient.Features.Sync.Application;
using HairCarePlus.Client.Patient.Features.Sync.Infrastructure;
using HairCarePlus.Shared.Common;

#if IOS
using HairCarePlus.Client.Patient.Platforms.iOS.Effects;
using UIKit;
#endif

namespace HairCarePlus.Client.Patient;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
                .ConfigureMauiHandlers(handlers =>
                {
                    handlers.AddHandler<CommunityToolkit.Maui.Views.CameraView, CommunityToolkit.Maui.Core.Handlers.CameraViewHandler>();
                })
                .UseSkiaSharp()
			.RegisterCalendarRoutes()
			.RegisterPhotoCaptureRoutes()
			.RegisterProgressRoutes()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("FontAwesome/FontAwesome.ttf", "FontAwesome");
				fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
				fonts.AddFont("MaterialSymbolsOutlined.ttf", "MaterialSymbolsOutlined");
			});

		// Add services to the container
		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "haircare.db");
		builder.Services.AddDbContextFactory<AppDbContext>(options =>
		{
			options.UseSqlite($"Data Source={dbPath}");
			// Only enable sensitive data logging in DEBUG
#if DEBUG
			options.EnableSensitiveDataLogging();
			// Comment out or reduce the LogTo level to avoid excessive logs
			// options.LogTo(message => Debug.WriteLine(message), LogLevel.Information); // Example: Log only Information and above
			// options.LogTo(Console.WriteLine, LogLevel.Warning); // Or log only warnings and above to console
#endif
		});
		builder.Services.AddPooledDbContextFactory<AppDbContext>(options =>
		{
			options.UseSqlite($"Data Source={dbPath}");
#if DEBUG
			options.EnableSensitiveDataLogging();
#endif
		});

		// Register toolkit camera provider
				
            // Register internal CameraProvider via reflection (required for CameraViewHandler)
            var providerType = typeof(CommunityToolkit.Maui.Views.CameraView).Assembly.GetType("CommunityToolkit.Maui.Core.CameraProvider");
            if (providerType is not null)
                builder.Services.AddSingleton(typeof(CommunityToolkit.Maui.Core.ICameraProvider), providerType);

            // Register infrastructure services
		builder.Services.AddSingleton<ILocalStorageService, LocalStorageService>();
		builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();
		builder.Services.AddSingleton<IMediaFileSystemService, FileSystemService>();
        builder.Services.AddSingleton<IUploadService, UploadService>();
        builder.Services.AddSingleton<HairCarePlus.Client.Patient.Infrastructure.Services.Interfaces.IProfileService, HairCarePlus.Client.Patient.Infrastructure.Services.ProfileService>();
		builder.Services.AddSingleton<INavigationService, NavigationService>();
#if ANDROID
		builder.Services.AddSingleton<IKeyboardService, HairCarePlus.Client.Patient.Platforms.Android.Services.KeyboardService>();
#elif IOS
		builder.Services.AddSingleton<IKeyboardService, HairCarePlus.Client.Patient.Platforms.iOS.Services.KeyboardService>();
#endif

		// Register Chat feature (repositories & sync) + presentation layer
		builder.Services.AddChatFeature();             // domain & infrastructure (extension)
		builder.Services.AddSingleton<Infrastructure.Network.Chat.IChatHubConnection, Infrastructure.Network.Chat.SignalRChatHubConnection>();
		builder.Services.AddTransient<ChatPage>();     // UI
		builder.Services.AddTransient<ChatViewModel>();

		// Register Calendar feature (handles its own DI, ViewModels и проч.)
		builder.Services.AddCalendarServices();

		// Register PhotoCapture feature
		builder.Services.AddPhotoCaptureFeature();

		// Register Progress feature
		builder.Services.AddProgressFeature();

		// Register Sync (Outbox + scheduler)
		builder.Services.AddSyncFeature();

		// Register startup tasks
		builder.Services.AddStartupTasks();

		// Register IMessenger singleton using WeakReferenceMessenger.Default
		builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

		// Sync Services
		builder.Services.AddSingleton<HairCarePlus.Shared.Communication.IOutboxRepository, OutboxRepository>();
		builder.Services.AddHttpClient<ISyncHttpClient, SyncHttpClient>(client =>
		{
			var apiBaseUrl = EnvironmentHelper.GetBaseApiUrl();
			client.BaseAddress = new Uri($"{apiBaseUrl}/");
			// Limit the first sync attempt to a reasonable timeout to avoid long startup hangs
			client.Timeout = TimeSpan.FromSeconds(30);
		});
		builder.Services.AddSingleton<ISyncService, SyncService>();
		builder.Services.AddHostedService<SyncScheduler>();


#if IOS
		EditorHandler.Mapper.AppendToMapping("NoKeyboardAccessory", (handler, view) =>
		{
			if (handler.PlatformView is UIKit.UITextView textView)
			{
				textView.InputAccessoryView = null;
			}
		});
		// Disable default gray highlight for CollectionView cells globally (background only)
		UICollectionViewCell.Appearance.BackgroundColor = UIColor.Clear;
#endif

#if DEBUG
		builder.Logging.ClearProviders();
		builder.Logging.AddConsole();
		builder.Logging.AddDebug()
			// Suppress noisy BindingDiagnostics warnings
			.AddFilter("Microsoft.Maui.Controls.Xaml.Diagnostics.BindingDiagnostics", LogLevel.None)
			// Reduce verbosity of EF Core to minimise log overhead during development
			.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning)
			.AddFilter("Microsoft.EntityFrameworkCore.ChangeTracking", LogLevel.Warning)
			.AddFilter("Microsoft.EntityFrameworkCore.Database.Transaction", LogLevel.Warning)
			.AddFilter("Microsoft.EntityFrameworkCore.Update", LogLevel.Warning)
			// Keep other categories at Information for useful context without flooding the output
			.SetMinimumLevel(LogLevel.Information);
#endif

		var app = builder.Build();

		

		ServiceHelper.Initialize(app.Services);

		return app;
	}

	// NOTE: Presentation‑level converters for Calendar feature are registered inside
	// CalendarServiceExtensions.RegisterConverters(). Additional converters can be
	// placed there or in App.xaml resources; avoid duplicate registrations.
}
