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
using Syncfusion.Maui.Core.Hosting;
using Syncfusion.Maui.Scheduler;
using Syncfusion.Maui.TabView;
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
using HairCarePlus.Client.Patient.Features.Notifications.Services.Implementation;
using System;
using System.IO;
using System.Diagnostics;

#if IOS
using HairCarePlus.Client.Patient.Platforms.iOS.Effects;
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
			.ConfigureSyncfusionCore()
			.RegisterCalendarRoutes()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("FontAwesome/FontAwesome.ttf", "FontAwesome");
				fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
			});

		// Add services to the container
		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "haircare.db");
		builder.Services.AddDbContextFactory<AppDbContext>(options =>
		{
			options.UseSqlite($"Data Source={dbPath}");
			options.EnableSensitiveDataLogging();
			options.LogTo(message => Debug.WriteLine(message));
		});
		builder.Services.AddDbContext<AppDbContext>(options =>
		{
			options.UseSqlite($"Data Source={dbPath}");
			options.EnableSensitiveDataLogging();
			options.LogTo(message => Debug.WriteLine(message));
		}, ServiceLifetime.Scoped);

		// Register infrastructure services
		builder.Services.AddSingleton<ILocalStorageService, LocalStorageService>();
		builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();
		builder.Services.AddSingleton<IMediaFileSystemService, FileSystemService>();
		builder.Services.AddSingleton<INavigationService, NavigationService>();
#if ANDROID
		builder.Services.AddSingleton<IKeyboardService, HairCarePlus.Client.Patient.Platforms.Android.Services.KeyboardService>();
#elif IOS
		builder.Services.AddSingleton<IKeyboardService, HairCarePlus.Client.Patient.Platforms.iOS.Services.KeyboardService>();
#endif

		// Register services
		builder.Services.AddSingleton<ICalendarService, CalendarServiceImpl>();
		builder.Services.AddSingleton<INotificationService, NotificationServiceImpl>();
		builder.Services.AddSingleton<IEventAggregationService, EventAggregationServiceImpl>();
		builder.Services.AddSingleton<IHairTransplantEventGenerator, HairTransplantEventGeneratorImpl>();

		// Register data initializers
		builder.Services.AddSingleton<IDataInitializer, CalendarDataInitializer>();

		// Register Pages and ViewModels
		builder.Services.AddTransient<ChatPage>();
		builder.Services.AddTransient<ChatViewModel>();

		// Register Calendar Feature
		builder.Services.AddCalendarServices();

		// Register ViewModels
		builder.Services.AddSingleton<TodayViewModel>();
		builder.Services.AddTransient<EventDetailViewModel>();

		// Register Pages
		builder.Services.AddSingleton<TodayPage>();
		builder.Services.AddTransient<EventDetailPage>();

#if IOS
		Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("NoKeyboardAccessory", (handler, view) =>
		{
			if (handler.PlatformView is UIKit.UITextView textView)
			{
				textView.InputAccessoryView = null;
			}
		});
#endif

#if DEBUG
		builder.Logging.AddDebug()
			.SetMinimumLevel(LogLevel.Debug);
#endif

		var app = builder.Build();
		ServiceHelper.Initialize(app.Services);

		return app;
	}

	private static void RegisterServices(IServiceCollection services)
	{
		// Calendar services
		services.AddSingleton<ICalendarService, CalendarServiceImpl>();
		services.AddSingleton<INotificationService, NotificationServiceImpl>();
	}
	
	private static void RegisterViewModels(IServiceCollection services)
	{
		// Calendar
		services.AddTransient<TodayViewModel>();
		services.AddTransient<EventDetailViewModel>();
		
		// Chat
		services.AddTransient<ChatViewModel>();
		
		// Calendar views
		services.AddTransient<TodayPage>();
		services.AddTransient<EventDetailPage>();
		
		// Chat views
		services.AddTransient<ChatPage>();
	}
	
	private static void RegisterConverters(IServiceCollection services)
	{
		// Register converters as resources
		Application.Current.Resources.Add("EventTypeToColorConverter", new EventTypeToColorConverter());
		Application.Current.Resources.Add("BoolToColorConverter", new BoolToColorConverter());
		Application.Current.Resources.Add("DoubleToPercentageConverter", new DoubleToPercentageConverter());
		Application.Current.Resources.Add("HasItemsConverter", new HasItemsConverter());
	}
}
