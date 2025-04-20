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
#if IOS
using HairCarePlus.Client.Patient.Platforms.iOS;
#endif

namespace HairCarePlus.Client.Patient.Features.Calendar.Views
{
    public partial class TodayPage : ContentPage
    {
        private readonly ILogger<TodayPage> _logger;
        private TodayViewModel _viewModel;
        
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
                
#if IOS
                // Remove default gray highlight on iOS CollectionView cells
                DateSelectorView?.DisableNativeHighlight();
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
                // Clear the CollectionView selection when the page appears
                if (DateSelectorView != null)
                {
                    DateSelectorView.SelectedItem = null;
                }
                
                // Ленивая инициализация: вызываем EnsureLoadedAsync (отработает только первый раз)
                await _viewModel.EnsureLoadedAsync();
                
                // Сбрасываем, затем снова задаём значение, чтобы гарантировать событие PropertyChanged
                _viewModel.ScrollToIndexTarget = null;
                _viewModel.ScrollToIndexTarget = _viewModel.SelectedDate;
                
                // Update visual state for the selected date
                if (_viewModel != null)
                {
                    UpdateSelectedDateVisualState(_viewModel.SelectedDate);
                }
                else
                {
                    _logger.LogWarning("ViewModel is null in OnAppearing, cannot update selected date visual state.");
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
        
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs? e)
        {
            if (e == null || _viewModel == null) return;
            
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
            if (_viewModel?.ScrollToIndexTarget.HasValue ?? false)
            {
                var targetDate = _viewModel.ScrollToIndexTarget.Value;
                // Reset the target immediately to prevent re-triggering
                _viewModel.ScrollToIndexTarget = null;

                _logger.LogInformation($"HandleScrollToIndexTargetChanged triggered for {targetDate.ToShortDateString()}");
                try
                {
                    // Ensure this runs on the UI thread and potentially after a delay
                    Dispatcher.DispatchAsync(async () =>
                    {
                        // Maybe increase delay slightly more as a precaution?
                        await Task.Delay(350); // Keep delay before finding index/item

                        // Find index based on the collection bound to DateSelectorView
                        var index = _viewModel?.SelectableDates.IndexOf(targetDate.Date) ?? -1;
                        
                        // Find the actual DateTime object in the source collection
                        var itemToSelect = _viewModel?.SelectableDates.FirstOrDefault(d => d.Date == targetDate.Date);

                        if (index >= 0 && DateSelectorView != null && _viewModel != null && itemToSelect != default(DateTime))
                        {
                            // Set SelectedItem *before* scrolling
                            _logger.LogInformation($"Setting DateSelectorView.SelectedItem to {((DateTime)itemToSelect).ToShortDateString()} BEFORE scrolling.");
                            DateSelectorView.SelectedItem = itemToSelect;

                            // Allow potential UI updates from setting SelectedItem
                            await Task.Delay(50); 

                            _logger.LogInformation($"Scrolling DateSelectorView to index {index} for {targetDate.ToShortDateString()}");
                            // Use CollectionView.ScrollTo with index and specify position
                            DateSelectorView.ScrollTo(index, position: ScrollToPosition.Center, animate: true);

                            // Try up to 3 times to apply visual state while container materializes
                            for (int attempt = 0; attempt < 3; attempt++)
                            {
                                await Task.Delay(120); // give UI time
                                UpdateSelectedDateVisualState(targetDate);
                                // If visual state applied (container found) we можем выйти
                                // Quick check: if any visible container now matches selected date and is in Selected state.
                                var hasApplied = DateSelectorView.LogicalChildren
                                    .OfType<Grid>()
                                    .Any(g => g.BindingContext is DateTime dt && dt.Date == targetDate.Date && VisualStateManager.GetVisualStateGroups(g)?.FirstOrDefault()?.CurrentState?.Name == "Selected");
                                if (hasApplied)
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                             if (itemToSelect == default(DateTime))
                             {
                                 _logger.LogWarning($"Could not find the DateTime object for {targetDate.ToShortDateString()} in SelectableDates to set SelectedItem.");
                             }
                             else
                             {
                                 _logger.LogWarning($"Could not find index for target date ({targetDate.ToShortDateString()}) or DateSelectorView/ViewModel is null.");
                             }
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
#if DEBUG
                _logger?.LogDebug("UpdateSelectedDateVisualState called for date: {Date}", selectedDate.Value.ToShortDateString());
#endif
                
                // Use the new DateSelectorView name
                if (DateSelectorView == null || DateSelectorView.ItemTemplate == null)
                {
#if DEBUG
                    _logger?.LogDebug("ItemTemplate or DateSelectorView is null");
#endif
                    return;
                }

                // Get visible containers from DateSelectorView
                var visibleContainers = DateSelectorView.LogicalChildren
                    .OfType<Grid>() // DateTemplate root is Grid
                    .Where(g => g.BindingContext is DateTime && g.IsVisible)
                    .ToList();

                if (!visibleContainers.Any())
                {
#if DEBUG
                    _logger?.LogDebug("No visible containers found");
#endif
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
#if DEBUG
                            _logger?.LogDebug("Updating container for date: {Date}, IsSelected: {IsSelected}", containerDate.ToShortDateString(), isSelected);
#endif
                        }
                        
                        var visualState = isSelected ? "Selected" : "Normal";
                        VisualStateManager.GoToState(container, visualState);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception in UpdateSelectedDateVisualState");
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