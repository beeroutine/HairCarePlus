using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Common.Behaviors;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.ComponentModel;
using Microsoft.Maui.Handlers;
using HairCarePlus.Client.Patient.Common.Utils;
#if IOS
using HairCarePlus.Client.Patient.Platforms.iOS;
#endif

namespace HairCarePlus.Client.Patient.Features.Calendar.Views
{
    public partial class TodayPage : ContentPage
    {
        private readonly ILogger<TodayPage> _logger;
        private TodayViewModel? _viewModel;
        
        public TodayPage(TodayViewModel viewModel, ILogger<TodayPage> logger)
        {
            try
            {
                _logger?.LogDebug("TodayPage constructor start");
                InitializeComponent();
                _logger?.LogDebug("TodayPage InitializeComponent completed");
                
                _viewModel = viewModel;
                _logger?.LogDebug("TodayViewModel retrieved from constructor");
                BindingContext = _viewModel;
                _logger?.LogDebug("BindingContext set to TodayViewModel");
                
                _logger = logger;
                _logger.LogInformation("TodayPage instance created");
                
                // iOS 17+/MAUI 9 already renders selection without gray overlay; no custom fix needed
                
#if IOS
                // Workaround: ensure iOS doesn't draw black overlay for selected CollectionView cell
                if (DateSelectorView != null)
                {
                    DateSelectorView.HandlerChanged += (_, __) =>
                    {
                        if (DateSelectorView.Handler?.PlatformView is UIKit.UICollectionView ui)
                        {
                            void ClearOverlay(UIKit.UICollectionView collection)
                            {
                                foreach (var cell in collection.VisibleCells)
                                    cell.SelectedBackgroundView = new UIKit.UIView { BackgroundColor = UIKit.UIColor.Clear };
                            }

                            // Apply immediately to currently visible cells
                            ClearOverlay(ui);

                            // Also clear overlay whenever user scrolls (new cells appear)
                            DateSelectorView.Scrolled += (_, __) => ClearOverlay(ui);
                            DateSelectorView.SelectionChanged += (_, __) => ClearOverlay(ui);
                        }
                    };
                }
#endif
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception in TodayPage constructor");
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
            else
            {
                _logger.LogWarning("ViewModel is null in OnAppearing, cannot subscribe to PropertyChanged.");
            }
            
            try
            {
                // Set SelectedItem explicitly before loading ensures the binding is established
                if (DateSelectorView != null && (DateSelectorView.SelectedItem as DateTime?) != _viewModel.SelectedDate)
                {
                    _logger.LogDebug("Explicitly setting DateSelectorView.SelectedItem in OnAppearing");
                    DateSelectorView.SelectedItem = _viewModel.SelectedDate;
                }
                
                await _viewModel.EnsureLoadedAsync();
                _logger.LogInformation("EnsureLoadedAsync completed in OnAppearing");

                await Task.Delay(150); // Delay for UI rendering
                _logger.LogDebug("Delay completed in OnAppearing");
                
                // Scroll the horizontal calendar to center the selected date after data loading
                if (DateSelectorView != null)
                {
                    try
                    {
                        _logger.LogDebug("Attempting ScrollTo in OnAppearing for date: {SelectedDate}", _viewModel.SelectedDate);
                        DateSelectorView.ScrollTo(_viewModel.SelectedDate, position: ScrollToPosition.Center, animate: false);
                        _logger.LogDebug("ScrollTo completed in OnAppearing");
                        
                        // Explicitly update visual states for visible items after scroll
                        await Task.Delay(50); // Short delay after scroll completes
                        UpdateVisibleItemStates(); 
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error scrolling DateSelectorView or updating states in OnAppearing");
                    }
                }
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
        
        private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs? e)
        {
            if (e?.PropertyName == nameof(TodayViewModel.SelectedDate) && DateSelectorView != null && _viewModel != null)
            {
                _logger.LogDebug("ViewModel SelectedDate changed to {SelectedDate}, updating UI.", _viewModel.SelectedDate);
                
                // Binding should handle setting SelectedItem, but setting explicitly can be safer
                if ((DateSelectorView.SelectedItem as DateTime?) != _viewModel.SelectedDate)
                {
                     DateSelectorView.SelectedItem = _viewModel.SelectedDate;
                }
                
                // Scroll to the item
                DateSelectorView.ScrollTo(_viewModel.SelectedDate, position: ScrollToPosition.Center, animate: true);
                
                // Add delay and explicit state update after scroll completes
                await Task.Delay(200); // Slightly longer delay for animated scroll
                UpdateVisibleItemStates();
            }
        }
        
        /// <summary>
        /// Iterates through visible cells in DateSelectorView and applies Normal/Selected state.
        /// </summary>
        private void UpdateVisibleItemStates()
        {
             if (DateSelectorView == null || _viewModel == null) return;
             
             _logger.LogDebug("Updating visual states for visible items. Target SelectedDate: {SelectedDate}", _viewModel.SelectedDate);
             int updatedCount = 0;
             try
             {
                foreach (var visual in DateSelectorView.VisibleCells())
                {
                    if (visual?.BindingContext is DateTime itemDate)
                    {
                        var targetState = itemDate.Date == _viewModel.SelectedDate.Date ? "Selected" : "Normal";
                        // _logger.LogTrace("Applying state '{TargetState}' to visual for date {ItemDate}", targetState, itemDate.ToShortDateString());
                        VisualStateManager.GoToState(visual, targetState);
                        updatedCount++;
                    }
                    else
                    {
                       // _logger.LogWarning("Visible cell found with null or non-DateTime BindingContext: {BindingContext}", visual?.BindingContext);
                    }
                }
                _logger.LogDebug("Finished updating visual states for {UpdatedCount} visible items.", updatedCount);
             }
             catch (Exception ex)
             {
                 _logger.LogError(ex, "Error updating visible item states.");
             }
        }
        
        private void OnCheckBoxChanged(object? sender, CheckedChangedEventArgs e)
        {
            if (sender is not CheckBox checkBox) return;
            
            var parent = checkBox.Parent;
            VisualElement? grid = null;
            
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
            if (checkBox.BindingContext is CalendarEvent calendarEvent && _viewModel != null)
            {
                _viewModel.ToggleEventCompletionCommand?.Execute(calendarEvent);
            }
        }
        
        private void OnCalendarDayLongPressed(object? sender, EventArgs e)
        {
            try
            {
                if (sender is Grid grid && grid.BindingContext is DateTime date && _viewModel != null)
                {
                    _viewModel.OpenMonthCalendarCommand?.Execute(date);
                    _logger?.LogDebug("OpenMonthCalendarCommand executed for date: {Date}", date.ToShortDateString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during long press on calendar day.");
            }
        }
        
        private void OnEventTapped(object? sender, TappedEventArgs e)
        {
            try
            {
                if (sender is Element element && element.BindingContext is CalendarEvent calendarEvent && _viewModel != null)
                {
                    _logger.LogInformation("Event tapped, executing ShowEventDetailsCommand for Event ID: {EventId}", calendarEvent.Id);
                    _viewModel.ShowEventDetailsCommand?.Execute(calendarEvent);
                }
                else
                {
                    _logger.LogWarning("Event tapped but sender or its binding context is not a CalendarEvent, or ViewModel is null.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling event tap.");
            }
        }
    }
} 