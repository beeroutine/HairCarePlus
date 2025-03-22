using System;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Devices;
using INotificationsService = HairCarePlus.Client.Patient.Features.Notifications.Services.Interfaces.INotificationService;

namespace HairCarePlus.Client.Patient.Features.Calendar.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CalendarPage : ContentPage
    {
        private CleanCalendarViewModel? ViewModel => BindingContext as CleanCalendarViewModel;
        private bool _isInitialized = false;
        private bool _isRefreshPending = false;

        public CalendarPage()
        {
            try
            {
                InitializeComponent();
                
                // Get services from DI container
                var serviceProvider = IPlatformApplication.Current.Services;
                var calendarService = serviceProvider.GetRequiredService<ICalendarService>();
                var notificationService = serviceProvider.GetRequiredService<INotificationsService>();
                var eventAggregationService = serviceProvider.GetRequiredService<IEventAggregationService>();
                
                // Create and set ViewModel
                BindingContext = new CleanCalendarViewModel(calendarService, notificationService, eventAggregationService);
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing CalendarPage: {ex}");
                MainThread.BeginInvokeOnMainThread(async () => {
                    await DisplayAlert("Error", "There was a problem loading the calendar. Please try again.", "OK");
                });
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            if (!_isInitialized) return;
            
            try
            {
                // Set flag to avoid multiple refreshes
                if (_isRefreshPending) return;
                _isRefreshPending = true;
                
                // Wait for layout to complete
                await Task.Delay(250);
                
                if (ViewModel?.RefreshCommand != null)
                {
                    // Execute refresh on main thread
                    MainThread.BeginInvokeOnMainThread(() => {
                        ViewModel.RefreshCommand.Execute(null);
                        
                        // Try to refresh any MonthCalendarView if present
                        var calendarView = FindMonthCalendarView();
                        if (calendarView != null)
                        {
                            calendarView.RefreshTapGestures();
                        }
                        
                        _isRefreshPending = false;
                    });
                }
                else
                {
                    _isRefreshPending = false;
                }
            }
            catch (Exception ex)
            {
                _isRefreshPending = false;
                System.Diagnostics.Debug.WriteLine($"Error in OnAppearing: {ex}");
            }
        }
        
        // Find the MonthCalendarView in the visual tree
        private MonthCalendarView? FindMonthCalendarView()
        {
            MonthCalendarView? result = null;
            
            void SearchInElement(Element element)
            {
                if (element is MonthCalendarView calendarView)
                {
                    result = calendarView;
                    return;
                }
                
                if (element is Layout layout)
                {
                    foreach (var child in layout.Children)
                    {
                        if (child is Element childElement)
                        {
                            SearchInElement(childElement);
                            if (result != null) return;
                        }
                    }
                }
                
                if (element is ContentView contentView && contentView.Content is Element contentElement)
                {
                    SearchInElement(contentElement);
                }
                
                if (element is ScrollView scrollView && scrollView.Content is Element scrollContent)
                {
                    SearchInElement(scrollContent);
                }
            }
            
            SearchInElement(this);
            return result;
        }

        private void OnEventCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            try
            {
                if (sender is CheckBox checkBox && checkBox.BindingContext is CalendarEvent calendarEvent && ViewModel != null)
                {
                    var command = ViewModel.MarkEventCompletedCommand;
                    if (command?.CanExecute(calendarEvent) == true)
                    {
                        command.Execute(calendarEvent);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnEventCheckedChanged: {ex}");
            }
        }
    }
} 