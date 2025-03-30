using HairCarePlus.Client.Patient.Features.Profile.Views;
using HairCarePlus.Client.Patient.Features.Doctor.Views;
using HairCarePlus.Client.Patient.Features.Calendar.Views;
using HairCarePlus.Client.Patient.Features.TreatmentProgress.Views;
using Microsoft.Maui.Controls;
using System;
using System.Diagnostics;

namespace HairCarePlus.Client.Patient;

public partial class AppShell : Shell
{
	public AppShell()
	{
		try
		{
			Debug.WriteLine("AppShell constructor start");
			InitializeComponent();
			Debug.WriteLine("AppShell InitializeComponent completed");
			
			RegisterRoutes();
			Debug.WriteLine("AppShell RegisterRoutes completed");
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Exception in AppShell constructor: {ex.Message}");
			Debug.WriteLine($"Stack trace: {ex.StackTrace}");
		}
	}

	private void RegisterRoutes()
	{
		try
		{
			Debug.WriteLine("RegisterRoutes start");
			
			// Профиль
			Routing.RegisterRoute(nameof(ProfilePage), typeof(ProfilePage));
			Debug.WriteLine("Registered route for ProfilePage");
			
			// Чат с доктором
			Routing.RegisterRoute(nameof(DoctorChatPage), typeof(DoctorChatPage));
			Debug.WriteLine("Registered route for DoctorChatPage");
			
			// Календарь и страницы календаря
			Routing.RegisterRoute(nameof(TodayPage), typeof(TodayPage));
			Debug.WriteLine("Registered route for TodayPage");
			
			Routing.RegisterRoute(nameof(CalendarPage), typeof(CalendarPage));
			Debug.WriteLine("Registered route for CalendarPage");
			
			Routing.RegisterRoute(nameof(EventDetailPage), typeof(EventDetailPage));
			Debug.WriteLine("Registered route for EventDetailPage");
			
			// Прогресс лечения
			Routing.RegisterRoute("TreatmentProgressPage", typeof(HairCarePlus.Client.Patient.Features.TreatmentProgress.Views.TreatmentProgressPage));
			Debug.WriteLine("Registered route for TreatmentProgressPage");
			
			Debug.WriteLine("RegisterRoutes completed successfully");
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Exception in RegisterRoutes: {ex.Message}");
			Debug.WriteLine($"Stack trace: {ex.StackTrace}");
		}
	}

	private async void OnChatToolbarItemClicked(object sender, EventArgs e)
	{
		try
		{
			Debug.WriteLine("OnChatToolbarItemClicked start");
			await Shell.Current.GoToAsync(nameof(DoctorChatPage));
			Debug.WriteLine("Navigated to DoctorChatPage");
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Exception in OnChatToolbarItemClicked: {ex.Message}");
			Debug.WriteLine($"Stack trace: {ex.StackTrace}");
		}
	}
}
