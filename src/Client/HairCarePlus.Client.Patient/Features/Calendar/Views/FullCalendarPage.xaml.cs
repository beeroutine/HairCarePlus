using System;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Xaml;
using INotificationsService = HairCarePlus.Client.Patient.Features.Notifications.Services.Interfaces.INotificationService;

namespace HairCarePlus.Client.Patient.Features.Calendar.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FullCalendarPage : ContentPage
    {
        private FullCalendarViewModel ViewModel => BindingContext as FullCalendarViewModel;

        public FullCalendarPage()
        {
            try
            {
                InitializeComponent();
                
                SetupBindingContext();
                
                // Регистрируем обработчик свайпов для навигации по месяцам
                var leftSwipeGesture = new SwipeGestureRecognizer
                {
                    Direction = SwipeDirection.Left
                };
                leftSwipeGesture.Swiped += OnSwipeLeft;
                
                var rightSwipeGesture = new SwipeGestureRecognizer
                {
                    Direction = SwipeDirection.Right
                };
                rightSwipeGesture.Swiped += OnSwipeRight;
                
                Content.GestureRecognizers.Add(leftSwipeGesture);
                Content.GestureRecognizers.Add(rightSwipeGesture);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing FullCalendarPage: {ex}");
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert("Ошибка", "Произошла ошибка при загрузке календаря", "OK");
                });
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // Принудительно обновляем календарь при отображении страницы
            ViewModel?.RefreshCommand?.Execute(null);
        }
        
        private void SetupBindingContext()
        {
            try 
            {
                // Получаем сервисы из DI-контейнера
                var serviceProvider = IPlatformApplication.Current.Services;
                var calendarService = serviceProvider.GetRequiredService<ICalendarService>();
                var notificationService = serviceProvider.GetRequiredService<INotificationsService>();
                var eventAggregationService = serviceProvider.GetRequiredService<IEventAggregationService>();
                
                // Создаем ViewModel и устанавливаем BindingContext
                BindingContext = new FullCalendarViewModel(
                    calendarService, 
                    notificationService, 
                    eventAggregationService);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting up BindingContext: {ex}");
                throw;
            }
        }
        
        private void OnSwipeLeft(object sender, SwipedEventArgs e)
        {
            if (ViewModel?.IsMonthViewVisible == true)
            {
                ViewModel.NextMonthCommand.Execute(null);
            }
        }
        
        private void OnSwipeRight(object sender, SwipedEventArgs e)
        {
            if (ViewModel?.IsMonthViewVisible == true)
            {
                ViewModel.PreviousMonthCommand.Execute(null);
            }
        }
    }
} 