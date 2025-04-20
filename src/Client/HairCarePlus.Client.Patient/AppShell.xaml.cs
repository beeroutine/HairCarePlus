using HairCarePlus.Client.Patient.Features.Chat.Views;
using HairCarePlus.Client.Patient.Features.Calendar.Views;
using Microsoft.Maui.Controls;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace HairCarePlus.Client.Patient;

public partial class AppShell : Shell
{
	public AppShell()
	{
		try
		{
			// AppShell constructor start (debug log removed)
			
			// Инициализация компонентов
			InitializeComponent();
			// InitializeComponent completed (debug log removed)
			
			RegisterRoutes();
			// RegisterRoutes completed (debug log removed)
		}
		catch (Exception ex)
		{
			// Exception details logging removed
		}
	}
	
	private void RegisterRoutes()
	{
		try
		{
			// RegisterRoutes start
			
			// Чат с доктором
			Routing.RegisterRoute(nameof(ChatPage), typeof(ChatPage));
			// Registered route for ChatPage
			
			// Календарь и страницы календаря
			Routing.RegisterRoute(nameof(TodayPage), typeof(TodayPage));
			// Registered route for TodayPage
			
			Routing.RegisterRoute(nameof(EventDetailPage), typeof(EventDetailPage));
			// Registered route for EventDetailPage
			
			// RegisterRoutes completed successfully
		}
		catch (Exception ex)
		{
			// Exception in RegisterRoutes logging removed
		}
	}

	private async void OnChatToolbarItemClicked(object sender, EventArgs e)
	{
		try
		{
			// OnChatToolbarItemClicked start
			await Shell.Current.GoToAsync(nameof(ChatPage));
			// Navigated to ChatPage
		}
		catch (Exception ex)
		{
			// Exception in OnChatToolbarItemClicked logging removed
		}
	}
}
