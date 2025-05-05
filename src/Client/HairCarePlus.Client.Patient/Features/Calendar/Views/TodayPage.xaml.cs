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
using System.Threading;

namespace HairCarePlus.Client.Patient.Features.Calendar.Views
{
    public partial class TodayPage : ContentPage
    {
        private readonly ILogger<TodayPage> _logger;
        private TodayViewModel? _viewModel;
        private bool _isUpdatingFromScroll = false; // flag to avoid recursive scroll/selection updates
        private bool _ignoreNextScrollEvent = false; // skip header update for programmatic scroll
        private CancellationTokenSource? _headerUpdateCts; // debounce token for month header update
        private DateTime _pendingVisibleDate;
        
        private bool _headerLocked = false; // lock header updates during programmatic animation until next user gesture
        
        // long-press cancellation token
        private CancellationTokenSource? _longPressCts;
        
        public TodayPage(TodayViewModel viewModel, ILogger<TodayPage> logger)
        {
            try
            {
                _logger = logger; // assign first to allow early logging
                _logger.LogDebug("TodayPage constructor start");
                InitializeComponent();
                _logger.LogDebug("TodayPage InitializeComponent completed");
                
                _viewModel = viewModel;
                _logger.LogDebug("TodayViewModel retrieved from constructor");
                BindingContext = _viewModel;
                _logger.LogDebug("BindingContext set to TodayViewModel");
                
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

                // Hook scrolling event to keep Month/Year header in sync when user swipes the horizontal calendar
                // RESTORED: Hook scroll event again
                if (DateSelectorView != null)
                {
                   DateSelectorView.Scrolled += OnDateSelectorScrolled;
                }
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

                // Ensure header reflects today's month/year immediately
                _viewModel.VisibleDate = _viewModel.SelectedDate;

                await Task.Delay(150); // Delay for UI rendering
                _logger.LogDebug("Delay completed in OnAppearing");
                
                // Scroll the horizontal calendar to center the selected date after data loading
                if (DateSelectorView != null)
                {
                    try
                    {
                        _logger.LogDebug("Attempting ScrollTo in OnAppearing for date: {SelectedDate}", _viewModel.SelectedDate);
                        _ignoreNextScrollEvent = false;
                        _ = CenterSelectedDateAsync();
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

            // cancel any pending header update to avoid memory leaks
            // RESTORED: Cancellation logic for debounce token
             _headerUpdateCts?.Cancel();
             _headerUpdateCts = null;
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
                
                // Set VisibleDate immediately via Dispatcher to update header when a date is TAPPED
                await Application.Current.MainPage.Dispatcher.DispatchAsync(() =>
                {
                  _viewModel.VisibleDate = _viewModel.SelectedDate; 
                });
                
                // RESTORED: Locking mechanism for scroll handler during animation
                 _headerLocked = true;
                 _ = Task.Run(async () =>
                 {
                     try
                     {
                         await Task.Delay(650); // Increased delay for animation + settling
                         await Application.Current.MainPage.Dispatcher.DispatchAsync(() =>
                         {
                             // Unlock header updates after animation finishes
                             _headerLocked = false;
                         });
                     }
                     catch (TaskCanceledException) { }
                 });
                
                // Scroll to the item with animation
                // RESTORED: Flag to ignore the first scroll event from programmatic scroll
                 _ignoreNextScrollEvent = true; 
                 _ = CenterSelectedDateAsync(); // Still scroll to center
                
                // Add delay and explicit state update after scroll completes
                await Task.Delay(200); // Slightly longer delay for animated scroll
                UpdateVisibleItemStates(); // Keep this to ensure visual selection is correct
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
                _logger.LogInformation("Checkbox toggled for Event ID {EventId}. New state: {State}", calendarEvent.Id, checkBox.IsChecked);
                if (!_viewModel.ToggleEventCompletionCommand.CanExecute(calendarEvent))
                {
                    _logger.LogWarning("ToggleEventCompletionCommand cannot execute for Event ID {EventId}", calendarEvent.Id);
                }
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

        // RESTORED: Method OnDateSelectorScrolled(object? sender, ItemsViewScrolledEventArgs e)
        private void OnDateSelectorScrolled(object? sender, ItemsViewScrolledEventArgs e)
        {
            try
            {
                _logger.LogTrace("Scrolled: first={First}, last={Last}, delta={Delta}, freeze={Freeze}, prog={Prog}", e.FirstVisibleItemIndex, e.LastVisibleItemIndex, e.HorizontalDelta, _headerLocked, _ignoreNextScrollEvent);
                if (_ignoreNextScrollEvent)
                {
                    _ignoreNextScrollEvent = false;
                    return;
                }

                if (_headerLocked)
                {
                    return; // ignore updates during freeze
                }

                if (_isUpdatingFromScroll) return; // guard against reentrancy
                if (DateSelectorView == null || _viewModel == null) return;

                var list = _viewModel.SelectableDates;
                if (e.FirstVisibleItemIndex < 0 || e.LastVisibleItemIndex < 0 ||
                    e.FirstVisibleItemIndex >= list.Count || e.LastVisibleItemIndex >= list.Count) return;

                // Determine central index of visible range
                int centerIdx = e.CenterItemIndex;
                if (centerIdx < 0)
                {
                    centerIdx = (e.FirstVisibleItemIndex + e.LastVisibleItemIndex) / 2;
                }
                if (centerIdx < 0 || centerIdx >= list.Count) return;

                var candidate = list[centerIdx];
                _logger.LogTrace("Candidate date for header: {Candidate}", candidate.ToShortDateString());

                if (!_headerLocked && (candidate.Month != _viewModel.VisibleDate.Month || candidate.Year != _viewModel.VisibleDate.Year))
                {
                    // Debounce header update 120ms
                    _pendingVisibleDate = candidate;
                    _headerUpdateCts?.Cancel();
                    _headerUpdateCts = new CancellationTokenSource();
                    var token = _headerUpdateCts.Token;
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(120, token);
                            if (token.IsCancellationRequested) return;
                            await Application.Current.MainPage.Dispatcher.DispatchAsync(() =>
                            {
                                _isUpdatingFromScroll = true;
                                _viewModel.VisibleDate = _pendingVisibleDate;
                                _isUpdatingFromScroll = false;
                            });
                        }
                        catch (TaskCanceledException) { }
                    }, token);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in OnDateSelectorScrolled");
            }
        }

        private async Task CenterSelectedDateAsync()
        {
            if (DateSelectorView == null || _viewModel == null) return;
            try
            {
                // CollectionView in .NET MAUI 9 exposes synchronous ScrollTo* methods (no *Async overload).
                // We therefore invoke the non‑async overload and immediately yield control so that callers
                // can still await this method without blocking the UI thread.
                DateSelectorView.ScrollTo(_viewModel.SelectedDate, position: ScrollToPosition.Center, animate: true);
                await Task.Yield(); // ensure asynchronous signature is preserved
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, "Error centering date");
            }
        }

        #region Long-Press handlers
        private void OnEventCardPressed(object? sender, EventArgs e)
        {
            try
            {
                if (sender is Element el && el.BindingContext is CalendarEvent evt && _viewModel != null)
                {
                    _longPressCts?.Cancel();
                    _longPressCts = new CancellationTokenSource();
                    var token = _longPressCts.Token;

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(2000, token); // 2-second press
                            if (token.IsCancellationRequested) return;

                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                if (_viewModel.ToggleEventCompletionCommand.CanExecute(evt))
                                {
                                    _viewModel.ToggleEventCompletionCommand.Execute(evt);
                                }
                            });
                        }
                        catch (TaskCanceledException) { }
                    }, token);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in OnEventCardPressed");
            }
        }

        private void OnEventCardReleased(object? sender, EventArgs e)
        {
            _longPressCts?.Cancel();
        }
        #endregion

        private void OnSwipeItemDoneInvoked(object? sender, EventArgs e)
        {
            try
            {
                if (sender is SwipeItem swipeItem && swipeItem.BindingContext is CalendarEvent evt && _viewModel != null)
                {
                    // execute completion command
                    if (_viewModel.ToggleEventCompletionCommand.CanExecute(evt))
                    {
                        _viewModel.ToggleEventCompletionCommand.Execute(evt);
                    }
                }

                // Close SwipeView regardless
                if (sender is SwipeItem { Parent: SwipeItems { Parent: SwipeView sv } })
                {
                    sv.Close();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in OnSwipeItemDoneInvoked");
            }
        }
    }
} 