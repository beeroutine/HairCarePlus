using Microsoft.Maui.Controls;
using System;
using System.Diagnostics;

namespace HairCarePlus.Client.Patient;

public partial class App : Application
{
	public App()
	{
		try
		{
			Debug.WriteLine("App constructor start");
			InitializeComponent();
			Debug.WriteLine("App InitializeComponent completed");
			
			MainPage = new AppShell();
			Debug.WriteLine("AppShell initialized and set as MainPage");
			
			RegisterServices();
			Debug.WriteLine("RegisterServices completed");
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Exception in App constructor: {ex.Message}");
			Debug.WriteLine($"Stack trace: {ex.StackTrace}");
			// You could set a simple error page here if needed
		}
	}

	private void RegisterServices()
	{
		// Placeholder for any service registration that might be needed
	}

	protected override void OnStart()
	{
		Debug.WriteLine("App.OnStart called");
		base.OnStart();
	}

	protected override void OnSleep()
	{
		Debug.WriteLine("App.OnSleep called");
		base.OnSleep();
	}

	protected override void OnResume()
	{
		Debug.WriteLine("App.OnResume called");
		base.OnResume();
	}
}
