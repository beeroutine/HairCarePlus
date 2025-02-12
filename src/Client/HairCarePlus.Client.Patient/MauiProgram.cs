using HairCarePlus.Client.Patient.Features.Profile.Services;
using HairCarePlus.Client.Patient.Features.Profile.ViewModels;
using HairCarePlus.Client.Patient.Features.Profile.Views;
using HairCarePlus.Client.Patient.Features.PhotoReport.ViewModels;
using HairCarePlus.Client.Patient.Features.PhotoReport.Views;
using HairCarePlus.Client.Patient.Features.Doctor.ViewModels;
using HairCarePlus.Client.Patient.Features.Doctor.Views;
using HairCarePlus.Client.Patient.Features.TreatmentProgress.ViewModels;
using HairCarePlus.Client.Patient.Features.TreatmentProgress.Views;
using HairCarePlus.Client.Patient.Features.DailyRoutine.ViewModels;
using HairCarePlus.Client.Patient.Features.DailyRoutine.Views;
using HairCarePlus.Client.Patient.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Client.Patient;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("FontAwesome.ttf", "FontAwesome");
			});

		// Register Services
		builder.Services.AddSingleton<INavigationService, NavigationService>();
		builder.Services.AddSingleton<INetworkService, NetworkService>();
		builder.Services.AddSingleton<ILocalStorageService, LocalStorageService>();
		builder.Services.AddSingleton<IProfileService, ProfileService>();
#if ANDROID
		builder.Services.AddSingleton<IVibrationService, HairCarePlus.Client.Patient.Platforms.Android.Services.VibrationService>();
#elif IOS
		builder.Services.AddSingleton<IVibrationService, HairCarePlus.Client.Patient.Platforms.iOS.Services.VibrationService>();
#endif

		// Register Pages and ViewModels
		builder.Services.AddTransient<ProfilePage>();
		builder.Services.AddTransient<ProfileViewModel>();
		builder.Services.AddTransient<PhotoReportPage>();
		builder.Services.AddTransient<PhotoReportViewModel>();
		builder.Services.AddTransient<DoctorChatPage>();
		builder.Services.AddTransient<DoctorChatViewModel>();
		builder.Services.AddTransient<DailyRoutinePage>();
		builder.Services.AddTransient<DailyRoutineViewModel>();

		// Register Treatment Progress
		builder.Services.AddTransient<TreatmentProgressPage>();
		builder.Services.AddTransient<TreatmentProgressViewModel>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
