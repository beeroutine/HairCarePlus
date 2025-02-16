using HairCarePlus.Client.Patient.Features.Profile.Views;
using HairCarePlus.Client.Patient.Features.Doctor.Views;

namespace HairCarePlus.Client.Patient;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		RegisterRoutes();
	}

	private void RegisterRoutes()
	{
		Routing.RegisterRoute("profile", typeof(ProfilePage));
		Routing.RegisterRoute("//chat", typeof(DoctorChatPage));
		// Здесь будут добавляться другие маршруты по мере разработки
	}

	private async void OnChatToolbarItemClicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("doctorChat");
	}
}
