using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Clinic;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		MainPage = new AppShell();
	}
}
