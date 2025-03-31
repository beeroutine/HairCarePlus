using HairCarePlus.Client.Patient.Features.Chat.ViewModels;
using HairCarePlus.Client.Patient.Features.Chat.Views;
using HairCarePlus.Client.Patient.Infrastructure.Services;
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
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;

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

		// Register Services
		builder.Services.AddSingleton<INavigationService, NavigationService>();
		builder.Services.AddSingleton<INetworkService, NetworkService>();
		builder.Services.AddSingleton<ILocalStorageService, LocalStorageService>();
		
		// Register HttpClient
		builder.Services.AddSingleton<HttpClient>(serviceProvider => new HttpClient {
			BaseAddress = new Uri("http://localhost:5281/")
		});
		
#if ANDROID
		builder.Services.AddSingleton<IVibrationService, HairCarePlus.Client.Patient.Platforms.Android.Services.VibrationService>();
		builder.Services.AddSingleton<IKeyboardService, HairCarePlus.Client.Patient.Platforms.Android.Services.KeyboardService>();
#elif IOS
		builder.Services.AddSingleton<IVibrationService, HairCarePlus.Client.Patient.Platforms.iOS.Services.VibrationService>();
		builder.Services.AddSingleton<IKeyboardService, HairCarePlus.Client.Patient.Platforms.iOS.Services.KeyboardService>();
#endif

		// Register Pages and ViewModels
		builder.Services.AddTransient<ChatPage>();
		builder.Services.AddTransient<ChatViewModel>();

		// Register Calendar Feature
		builder.Services.AddCalendarServices();

		// Register ViewModels
		builder.Services.AddTransient<TodayViewModel>();
		builder.Services.AddTransient<EventDetailViewModel>();

		// Register Pages
		builder.Services.AddTransient<TodayPage>();
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
		services.AddSingleton<ICalendarService, CalendarService>();
		services.AddSingleton<INotificationService, NotificationService>();
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
