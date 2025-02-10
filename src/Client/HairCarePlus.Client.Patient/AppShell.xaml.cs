using HairCarePlus.Client.Patient.Features.Profile.Views;

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
		// Здесь будут добавляться другие маршруты по мере разработки
	}
}
