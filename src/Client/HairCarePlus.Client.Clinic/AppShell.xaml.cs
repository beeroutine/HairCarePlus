using Microsoft.Maui.Controls;
using HairCarePlus.Client.Clinic.Features.Chat.Views;
using HairCarePlus.Client.Clinic.Features.Dashboard.Views;
using HairCarePlus.Client.Clinic.Features.Patient.Views;

namespace HairCarePlus.Client.Clinic;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		// 'DashboardPage' already provided via ShellContent in XAML, registering it again causes duplicates
		Routing.RegisterRoute(nameof(Features.Patient.Views.PatientPage), typeof(Features.Patient.Views.PatientPage));
		Routing.RegisterRoute(nameof(ChatPage), typeof(ChatPage));
		Routing.RegisterRoute(nameof(HairCarePlus.Client.Clinic.Features.Chat.Views.ChatPage), typeof(HairCarePlus.Client.Clinic.Features.Chat.Views.ChatPage));
		Routing.RegisterRoute("patient-progress", typeof(HairCarePlus.Client.Clinic.Features.Patient.Views.PatientProgressPage));
	}
}
