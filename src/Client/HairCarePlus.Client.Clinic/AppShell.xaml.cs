using Microsoft.Maui.Controls;
using HairCarePlus.Client.Clinic.Features.Chat.Views;

namespace HairCarePlus.Client.Clinic;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(ChatPage), typeof(ChatPage));
	}
}
