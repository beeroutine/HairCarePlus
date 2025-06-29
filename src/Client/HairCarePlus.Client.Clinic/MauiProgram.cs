using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using Microsoft.Maui.Storage;
using SkiaSharp.Views.Maui.Controls.Hosting;

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
		builder.Services.AddDbContext<Infrastructure.Storage.AppDbContext>(options =>
		    options.UseSqlite($"Data Source={dbPath}"));
		builder.Services.AddScoped<Features.Chat.Domain.IChatMessageRepository, Infrastructure.Features.Chat.Repositories.ChatMessageRepository>();

		builder.Services.AddTransient<Features.Dashboard.ViewModels.DashboardViewModel>();
		builder.Services.AddTransient<Features.Dashboard.Views.DashboardPage>();

		builder.Services.AddTransient<Features.Patient.Views.PatientPage>();

		builder.UseMauiCommunityToolkit();

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
		    ctx.Database.EnsureCreated();
		}

		return builder.Build();
	}
}
