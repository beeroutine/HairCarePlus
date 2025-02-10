using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		RegisterServices();
		MainPage = new AppShell();
	}

	private void RegisterServices()
	{
		// Здесь будет регистрация сервисов
	}
}
