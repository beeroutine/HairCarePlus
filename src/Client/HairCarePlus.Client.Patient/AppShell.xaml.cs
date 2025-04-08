using HairCarePlus.Client.Patient.Features.Chat.Views;
using HairCarePlus.Client.Patient.Features.Calendar.Views;
using Microsoft.Maui.Controls;
using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HairCarePlus.Client.Patient;

public partial class AppShell : Shell
{
	public AppShell()
	{
		try
		{
			Debug.WriteLine("AppShell constructor start");
			
			// Инициализация компонентов
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
			
			// Чат с доктором
			Routing.RegisterRoute(nameof(ChatPage), typeof(ChatPage));
			Debug.WriteLine("Registered route for ChatPage");
			
			// Календарь и страницы календаря
			Routing.RegisterRoute(nameof(TodayPage), typeof(TodayPage));
			Debug.WriteLine("Registered route for TodayPage");
			
			Routing.RegisterRoute(nameof(EventDetailPage), typeof(EventDetailPage));
			Debug.WriteLine("Registered route for EventDetailPage");
			
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
			await Shell.Current.GoToAsync(nameof(ChatPage));
			Debug.WriteLine("Navigated to ChatPage");
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Exception in OnChatToolbarItemClicked: {ex.Message}");
			Debug.WriteLine($"Stack trace: {ex.StackTrace}");
		}
	}
}
