using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Common.Behaviors;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace HairCarePlus.Client.Patient.Features.Calendar.Views
{
    public partial class TodayPage : ContentPage
    {
        private readonly ILogger<TodayPage> _logger;
        private bool _isLoaded;
        private TodayViewModel _viewModel;
        private bool _isDataLoaded = false;
        
        public TodayPage(TodayViewModel viewModel, ILogger<TodayPage> logger)
        {
            try
            {
                Debug.WriteLine("TodayPage constructor start");
                InitializeComponent();
                Debug.WriteLine("TodayPage InitializeComponent completed");
                
                _viewModel = viewModel;
                Debug.WriteLine("TodayViewModel retrieved from constructor");
                BindingContext = _viewModel;
                Debug.WriteLine("BindingContext set to TodayViewModel");
                
                _logger = logger;
                _logger.LogInformation("TodayPage instance created");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in TodayPage constructor: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _logger.LogInformation("TodayPage OnAppearing called");
            
            // Subscribe to ViewModel changes when the page appears
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged; // Ensure clean state
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
            
            try
            {
                // Clear both the CollectionView selection and visual states
                if (daysCollection != null)
                {
                    daysCollection.SelectedItem = null;
                }
                
                if (!_isDataLoaded)
                {
                    _logger.LogInformation("First time loading data for TodayPage");
                    // Add a small delay to ensure DbContext is fully initialized
                    await Task.Delay(100);
                    
                    // Use the ViewModel's dedicated method for initialization
                    await ((TodayViewModel)BindingContext).InitializeDataAsync();
                    
                    _isDataLoaded = true;
                }
                else
                {
                    _logger.LogInformation("Data already loaded, updating UI if needed");
                    // No specific refresh method available, just log the state
                    _logger.LogInformation("UI update skipped, no refresh method available");
                }
                
                // Update visual state for the selected date
                UpdateSelectedDateVisualState(_viewModel.SelectedDate);
                _logger.LogInformation("TodayPage OnAppearing completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnAppearing");
            }
        }
        
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _logger.LogInformation("TodayPage OnDisappearing called");
            
            // Отписываемся от событий при уходе со страницы
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
        }
        
        private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Когда SelectedDate изменяется, обновляем визуальное состояние
            if (e.PropertyName == nameof(TodayViewModel.SelectedDate))
            {
                UpdateSelectedDateVisualState(_viewModel.SelectedDate);
            }
            
            // Check if the scroll target property changed
            if (e.PropertyName == nameof(TodayViewModel.ScrollToIndexTarget))
            {
                HandleScrollToIndexTargetChanged();
            }
        }
        
        private void HandleScrollToIndexTargetChanged()
        {
            if (_viewModel.ScrollToIndexTarget.HasValue)
            {
                var targetDate = _viewModel.ScrollToIndexTarget.Value;
                // Reset the target immediately to prevent re-triggering 
                // (though Dispatcher might make this less critical, it's safer)
                _viewModel.ScrollToIndexTarget = null; 

                _logger.LogInformation($"HandleScrollToIndexTargetChanged triggered for {targetDate.ToShortDateString()}");
                try
                {
                    // Ensure this runs on the UI thread and potentially after a delay
                    Dispatcher.DispatchAsync(async () => 
                    {    
                        // Maybe increase delay slightly more as a precaution?
                        await Task.Delay(350); 
                        
                        // Define item width and spacing based on XAML
                        const double itemWidth = 55.0;
                        const double itemSpacing = 5.0;
                        const double totalItemWidth = itemWidth + itemSpacing;
                        
                        var index = _viewModel.CalendarDays.IndexOf(targetDate.Date);
                        
                        if (index >= 0 && daysScrollView != null)
                        {
                            double horizontalOffset = index * totalItemWidth;
                            _logger.LogInformation($"Scrolling daysScrollView to offset {horizontalOffset} for index {index} ({targetDate.ToShortDateString()})");
                            await daysScrollView.ScrollToAsync(horizontalOffset, 0, true); // Scroll horizontally
                        }
                        else
                        {
                            _logger.LogWarning($"Could not find index for target date ({targetDate.ToShortDateString()}) or daysScrollView is null.");
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling ScrollToIndexTarget change.");
                }
            }
        }
        
        private void UpdateSelectedDateVisualState(DateTime? selectedDate)
        {
            if (selectedDate == null)
                return;
                
            try
            {
                Debug.WriteLine($"UpdateSelectedDateVisualState called for date: {selectedDate.Value.ToShortDateString()}");
                
                if (daysCollection.ItemTemplate == null)
                {
                    Debug.WriteLine("ItemTemplate is null");
                    return;
                }

                // Get only visible containers using ItemsLayout information
                var visibleContainers = daysCollection.LogicalChildren
                    .OfType<Grid>()
                    .Where(g => g.BindingContext is DateTime && g.IsVisible)
                    .ToList();

                if (!visibleContainers.Any())
                {
                    Debug.WriteLine("No visible containers found");
                    return;
                }
                
                foreach (var container in visibleContainers)
                {
                    if (container.BindingContext is DateTime containerDate)
                    {
                        bool isSelected = containerDate.Date == selectedDate.Value.Date;
                        
                        // Only log if selected to reduce noise
                        if (isSelected)
                        {
                            Debug.WriteLine($"Updating container for date: {containerDate.ToShortDateString()}, IsSelected: {isSelected}");
                        }
                        
                        var visualState = isSelected ? "Selected" : "Normal";
                        VisualStateManager.GoToState(container, visualState);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in UpdateSelectedDateVisualState: {ex.Message}");
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