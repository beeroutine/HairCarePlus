using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Common.Behaviors;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Calendar.Views
{
    public partial class TodayPage : ContentPage
    {
        private TodayViewModel _viewModel;
        
        public TodayPage()
        {
            try
            {
                Debug.WriteLine("TodayPage constructor start");
                InitializeComponent();
                Debug.WriteLine("TodayPage InitializeComponent completed");
                
                _viewModel = ServiceHelper.GetService<TodayViewModel>();
                Debug.WriteLine("TodayViewModel retrieved from ServiceHelper");
                BindingContext = _viewModel;
                Debug.WriteLine("BindingContext set to TodayViewModel");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in TodayPage constructor: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        protected override void OnAppearing()
        {
            try
            {
                Debug.WriteLine("TodayPage OnAppearing start");
                base.OnAppearing();
                
                // Refresh selected date when returning to this page
                if (_viewModel != null)
                {
                    DateTime currentDate = _viewModel.SelectedDate;
                    _viewModel.SelectedDate = currentDate;
                    
                    // Принудительная загрузка данных
                    Task.Run(async () => 
                    {
                        await _viewModel.LoadTodayEventsAsync();
                        await _viewModel.LoadEventCountsForVisibleDaysAsync();
                        await _viewModel.CheckOverdueEventsAsync();
                        
                        // Добавляем отладочную информацию о загруженных событиях
                        var events = _viewModel.FlattenedEvents;
                        Debug.WriteLine($"Loaded {events?.Count ?? 0} events for date {currentDate.ToShortDateString()}");
                        if (events != null && events.Any())
                        {
                            foreach (var evt in events)
                            {
                                Debug.WriteLine($"Event: {evt.Title}, Type: {evt.EventType}, Time: {evt.Date.ToShortTimeString()}");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("No events found for selected date");
                        }
                    });
                    
                    Debug.WriteLine($"TodayPage OnAppearing refreshed selected date: {currentDate}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in TodayPage OnAppearing: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private void OnCheckBoxChanged(object sender, CheckedChangedEventArgs e)
        {
            var checkBox = (CheckBox)sender;
            var parent = checkBox.Parent;
            VisualElement grid = null;
            
            // Traverse up the visual tree until we find a Grid
            while (parent != null && grid == null)
            {
                grid = parent as Grid;
                parent = parent.Parent;
            }
            
            if (grid != null)
            {
                // Set the visual state based on the checkbox value
                VisualStateManager.GoToState(grid, checkBox.IsChecked ? "ItemCompleted" : "ItemNormal");
                
                // Also set visual state for the checkbox itself
                VisualStateManager.GoToState(checkBox, checkBox.IsChecked ? "CheckboxCompleted" : "CheckboxNormal");
            }
            
            // Get the binding context and execute the command
            if (checkBox.BindingContext is CalendarEvent calendarEvent)
            {
                _viewModel.ToggleEventCompletionCommand.Execute(calendarEvent);
            }
        }
        
        private void OnCalendarDayTapped(object sender, TappedEventArgs e)
        {
            try
            {
                if (sender is Grid grid && grid.BindingContext is DateTime date)
                {
                    _viewModel.SelectDateCommand.Execute(date);
                    Debug.WriteLine($"SelectDateCommand executed for date: {date.ToShortDateString()}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in OnCalendarDayTapped: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private void OnCalendarDayLongPressed(object sender, EventArgs e)
        {
            try
            {
                if (sender is Grid grid && grid.BindingContext is DateTime date)
                {
                    _viewModel.OpenMonthCalendarCommand.Execute(date);
                    Debug.WriteLine($"OpenMonthCalendarCommand executed for date: {date.ToShortDateString()}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in OnCalendarDayLongPressed: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private void OnEventTapped(object sender, TappedEventArgs e)
        {
            try
            {
                if (sender is Grid grid && grid.BindingContext is CalendarEvent calendarEvent)
                {
                    _viewModel.ViewEventDetailsCommand.Execute(calendarEvent);
                    Debug.WriteLine($"ViewEventDetailsCommand executed for event: {calendarEvent.Title}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in OnEventTapped: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
} 