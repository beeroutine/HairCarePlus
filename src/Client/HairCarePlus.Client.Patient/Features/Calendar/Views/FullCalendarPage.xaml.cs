using System;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Xaml;
using INotificationsService = HairCarePlus.Client.Patient.Features.Notifications.Services.Interfaces.INotificationService;

namespace HairCarePlus.Client.Patient.Features.Calendar.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FullCalendarPage : ContentPage
    {
        private FullCalendarViewModel ViewModel => BindingContext as FullCalendarViewModel;
        private readonly ILogger<FullCalendarPage> _logger;

        public FullCalendarPage()
        {
            try
            {
                InitializeComponent();
                
                // Получаем логгер из DI
                try 
                {
                    _logger = IPlatformApplication.Current.Services.GetService<ILogger<FullCalendarPage>>();
                    _logger?.LogInformation("FullCalendarPage initializing");
                }
                catch (Exception ex) 
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to get logger: {ex}");
                }
                
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
                
                _logger?.LogInformation("FullCalendarPage initialized successfully");
            }
            catch (Exception ex)
            {
                var exceptionDetails = $"Message: {ex.Message}, StackTrace: {ex.StackTrace}";
                if (ex.InnerException != null)
                {
                    exceptionDetails += $", Inner Exception: {ex.InnerException.Message}";
                }
                
                _logger?.LogError(ex, "Error initializing FullCalendarPage");
                System.Diagnostics.Debug.WriteLine($"Error initializing FullCalendarPage: {exceptionDetails}");
                
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert("Ошибка", $"Произошла ошибка при загрузке календаря: {ex.Message}", "OK");
                });
            }
        }

        protected override void OnAppearing()
        {
            try
            {
                base.OnAppearing();
                
                _logger?.LogInformation("FullCalendarPage OnAppearing");
                
                // Принудительно обновляем календарь при отображении страницы
                ViewModel?.RefreshCommand?.Execute(null);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in OnAppearing");
                System.Diagnostics.Debug.WriteLine($"Error in OnAppearing: {ex}");
            }
        }
        
        private void SetupBindingContext()
        {
            try 
            {
                _logger?.LogInformation("Setting up BindingContext");
                
                // Получаем сервисы из DI-контейнера
                var serviceProvider = IPlatformApplication.Current.Services;
                
                _logger?.LogInformation("Getting calendar service");
                var calendarService = serviceProvider.GetRequiredService<ICalendarService>();
                
                _logger?.LogInformation("Getting notification service");
                var notificationService = serviceProvider.GetRequiredService<INotificationsService>();
                
                _logger?.LogInformation("Getting event aggregation service");
                var eventAggregationService = serviceProvider.GetRequiredService<IEventAggregationService>();
                
                // Создаем ViewModel и устанавливаем BindingContext
                _logger?.LogInformation("Creating ViewModel");
                BindingContext = new FullCalendarViewModel(
                    calendarService, 
                    notificationService, 
                    eventAggregationService);
                
                _logger?.LogInformation("BindingContext set up successfully");
            }
            catch (Exception ex)
            {
                var exceptionDetails = $"Message: {ex.Message}, StackTrace: {ex.StackTrace}";
                if (ex.InnerException != null)
                {
                    exceptionDetails += $", Inner Exception: {ex.InnerException.Message}";
                }
                
                _logger?.LogError(ex, "Error setting up BindingContext");
                System.Diagnostics.Debug.WriteLine($"Error setting up BindingContext: {exceptionDetails}");
                throw;
            }
        }
        
        private void OnSwipeLeft(object sender, SwipedEventArgs e)
        {
            try
            {
                _logger?.LogInformation("OnSwipeLeft");
                if (ViewModel?.IsMonthViewVisible == true)
                {
                    ViewModel.NextMonthCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in OnSwipeLeft");
                System.Diagnostics.Debug.WriteLine($"Error in OnSwipeLeft: {ex}");
            }
        }
        
        private void OnSwipeRight(object sender, SwipedEventArgs e)
        {
            try
            {
                _logger?.LogInformation("OnSwipeRight");
                if (ViewModel?.IsMonthViewVisible == true)
                {
                    ViewModel.PreviousMonthCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in OnSwipeRight");
                System.Diagnostics.Debug.WriteLine($"Error in OnSwipeRight: {ex}");
            }
        }
    }
} 