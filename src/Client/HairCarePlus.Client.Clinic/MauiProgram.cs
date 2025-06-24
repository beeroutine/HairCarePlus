using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using CommunityToolkit.Maui;

namespace HairCarePlus.Client.Clinic;

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
				// Material Icons — used for various glyphs (e.g., reply arrow)
				fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
			});

		builder.Services.AddSingleton<Infrastructure.Network.Chat.IChatHubConnection, Infrastructure.Network.Chat.SignalRChatHubConnection>();
		builder.Services.AddTransient<Features.Chat.ViewModels.ChatViewModel>();
		builder.Services.AddTransient<Features.Chat.Views.ChatPage>();

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

		return builder.Build();
	}
}
