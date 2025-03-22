using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using INotificationsService = HairCarePlus.Client.Patient.Features.Notifications.Services.Interfaces.INotificationService;
using CommunityToolkit.Maui.Core;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    /// <summary>
    /// A timer to display and track active restrictions
    /// </summary>
    public class RestrictionTimerViewModel : ObservableObject
    {
        private CalendarEvent _restrictionEvent;
        private string _remainingTimeText;
        private double _progressPercentage;

        public CalendarEvent RestrictionEvent
        {
            get => _restrictionEvent;
            set => SetProperty(ref _restrictionEvent, value);
        }

        public string RemainingTimeText
        {
            get => _remainingTimeText;
            set => SetProperty(ref _remainingTimeText, value);
        }

        public double ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }

        public string Title => RestrictionEvent?.Title;
        public string Description => RestrictionEvent?.Description;
    }

    /// <summary>
    /// Clean implementation of the Calendar View Model that avoids ambiguity issues
    /// </summary>
    public partial class CleanCalendarViewModel : ObservableObject
    {
        private readonly ICalendarService _calendarService;
        private readonly INotificationsService _notificationService;
        private readonly IEventAggregationService _eventAggregationService;

        [ObservableProperty]
        private string _title = "Calendar";

        [ObservableProperty]
        private DateTime _selectedDate = DateTime.Today;

        [ObservableProperty]
        private DateTime _displayMonth = DateTime.Today;

        [ObservableProperty]
        private DateTime _currentMonthDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        [ObservableProperty]
        private ObservableCollection<CalendarEvent> _eventsForMonth = new();

        [ObservableProperty]
        private ObservableCollection<CalendarEvent> _eventsForSelectedDate = new();

        [ObservableProperty]
        private ObservableCollection<RestrictionTimerViewModel> _activeRestrictions = new();

        [ObservableProperty]
        private ObservableCollection<CalendarDayViewModel> _calendarDays = new();

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private string _errorMessage;

        [ObservableProperty]
        private int _retryCount;

        private const int MaxRetries = 3;

        // Commands
        public ICommand RefreshCommand { get; private set; }
        public ICommand GoToTodayCommand { get; private set; }
        public ICommand NextMonthCommand { get; private set; }
        public ICommand PreviousMonthCommand { get; private set; }
        public ICommand MarkEventCompletedCommand { get; private set; }
        public ICommand DaySelectedCommand { get; private set; }

        public CleanCalendarViewModel(
            ICalendarService calendarService,
            INotificationsService notificationService,
            IEventAggregationService eventAggregationService)
        {
            _calendarService = calendarService;
            _notificationService = notificationService;
            _eventAggregationService = eventAggregationService;

            // Initialize commands
            InitializeCommands();
            
            // Initialize
            UpdateCalendarDays();
            
            // Load events for today when first opening
            LoadEventsForSelectedDateAsync().ConfigureAwait(false);
        }

        partial void OnSelectedDateChanged(DateTime value)
        {
            // Don't use ConfigureAwait(false) here as we need to return to UI thread
            MainThread.BeginInvokeOnMainThread(async () => {
                await LoadEventsForSelectedDateAsync();
                UpdateCalendarDays();
            });
        }

        partial void OnDisplayMonthChanged(DateTime value)
        {
            MainThread.BeginInvokeOnMainThread(async () => {
                await LoadEventsForMonthAsync(value.Year, value.Month);
            });
        }

        partial void OnCurrentMonthDateChanged(DateTime value)
        {
            MainThread.BeginInvokeOnMainThread(async () => {
                await LoadEventsForMonthAsync(value.Year, value.Month);
                UpdateCalendarDays();
            });
        }

        private void InitializeCommands()
        {
            RefreshCommand = new Command(async () => await RefreshAsync());
            GoToTodayCommand = new Command(ExecuteGoToToday);
            NextMonthCommand = new Command(ExecuteNextMonth);
            PreviousMonthCommand = new Command(ExecutePreviousMonth);
            MarkEventCompletedCommand = new Command<CalendarEvent>(ExecuteMarkEventCompleted);
            DaySelectedCommand = new Command<DateTime>(ExecuteDaySelected);
        }

        private void ExecuteGoToToday()
        {
            CurrentMonthDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            SelectedDate = DateTime.Today;
        }

        private void ExecuteNextMonth()
        {
            CurrentMonthDate = CurrentMonthDate.AddMonths(1);
        }

        private void ExecutePreviousMonth()
        {
            CurrentMonthDate = CurrentMonthDate.AddMonths(-1);
        }

        private void ExecuteDaySelected(DateTime date)
        {
            // Just select the date, no popup needed
            SelectedDate = date;
        }

        private void ExecuteMarkEventCompleted(CalendarEvent calendarEvent)
        {
            MainThread.BeginInvokeOnMainThread(async () => {
                await MarkEventCompletedAsync(calendarEvent);
            });
        }

        private async Task RefreshAsync()
        {
            if (IsBusy) return;
            
            IsBusy = true;
            HasError = false;
            ErrorMessage = string.Empty;

            try
            {
                await LoadEventsForMonthAsync(DisplayMonth.Year, DisplayMonth.Month);
                await LoadEventsForSelectedDateAsync();
                await LoadActiveRestrictionsAsync();
                UpdateCalendarDays();
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Failed to load calendar data: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadEventsForMonthAsync(int year, int month)
        {
            if (IsBusy) return;
            
            IsBusy = true;
            HasError = false;
            ErrorMessage = string.Empty;

            try
            {
                var events = await _calendarService.GetEventsForMonthAsync(year, month);
                
                // Make sure UI updates happen on main thread
                MainThread.BeginInvokeOnMainThread(() => {
                    if (events != null)
                    {
                        EventsForMonth = new ObservableCollection<CalendarEvent>(events);
                    }
                    else
                    {
                        EventsForMonth = new ObservableCollection<CalendarEvent>();
                    }
                });
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Failed to load events: {ex.Message}";
                
                MainThread.BeginInvokeOnMainThread(() => {
                    EventsForMonth = new ObservableCollection<CalendarEvent>();
                });
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadEventsForSelectedDateAsync()
        {
            try
            {
                IsBusy = true;

                // Create a new collection to store all events
                var allDayEvents = new List<CalendarEvent>();

                // First attempt to load from service if available
                if (_calendarService != null)
                {
                    var events = await _calendarService.GetEventsForDateAsync(SelectedDate);
                    if (events != null)
                    {
                        allDayEvents.AddRange(events);
                    }
                }
                else
                {
                    // Fall back to filtering from the events for month
                    var filteredEvents = EventsForMonth.Where(e => e.Date.Date == SelectedDate.Date).ToList();
                    allDayEvents.AddRange(filteredEvents);
                }

                // Now add any active restrictions that are valid for the selected date
                if (_calendarService != null)
                {
                    var activeRestrictions = await _calendarService.GetActiveRestrictionsAsync();
                    foreach (var restriction in activeRestrictions)
                    {
                        // Only include restrictions that are active on the selected date
                        if (restriction.Date.Date <= SelectedDate.Date && 
                            (!restriction.ExpirationDate.HasValue || restriction.ExpirationDate.Value >= SelectedDate.Date))
                        {
                            // Add to the events collection if not already included
                            if (!allDayEvents.Any(e => e.Id == restriction.Id))
                            {
                                allDayEvents.Add(restriction);
                            }
                        }
                    }
                }
                else
                {
                    // Fall back to active restrictions collection
                    foreach (var timer in ActiveRestrictions)
                    {
                        if (timer.RestrictionEvent != null)
                        {
                            var restriction = timer.RestrictionEvent;
                            // Only include restrictions that are active on the selected date
                            if (restriction.Date.Date <= SelectedDate.Date && 
                                (!restriction.ExpirationDate.HasValue || restriction.ExpirationDate.Value >= SelectedDate.Date))
                            {
                                // Add to the events collection if not already included
                                if (!allDayEvents.Any(e => e.Id == restriction.Id))
                                {
                                    allDayEvents.Add(restriction);
                                }
                            }
                        }
                    }
                }

                // Replace the events collection with the combined data
                EventsForSelectedDate = new ObservableCollection<CalendarEvent>(allDayEvents);
                ErrorMessage = null;
            }
            catch (Exception ex)
            {
                HandleError("Error loading events for selected date", ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadActiveRestrictionsAsync()
        {
            try
            {
                var restrictions = await _calendarService.GetActiveRestrictionsAsync();
                var restrictionTimers = new ObservableCollection<RestrictionTimerViewModel>();
                
                if (restrictions != null)
                {
                    foreach (var restriction in restrictions)
                    {
                        restrictionTimers.Add(new RestrictionTimerViewModel { RestrictionEvent = restriction });
                    }
                }
                
                ActiveRestrictions = restrictionTimers;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load restrictions: {ex.Message}");
                ActiveRestrictions = new ObservableCollection<RestrictionTimerViewModel>();
            }
        }

        private void UpdateCalendarDays()
        {
            var firstDayOfMonth = new DateTime(CurrentMonthDate.Year, CurrentMonthDate.Month, 1);
            var firstDayOfCalendarView = firstDayOfMonth.AddDays(-(int)firstDayOfMonth.DayOfWeek);

            var calendarDays = new ObservableCollection<CalendarDayViewModel>();
            for (int i = 0; i < 42; i++) // 6 weeks x 7 days
            {
                var day = firstDayOfCalendarView.AddDays(i);
                var isCurrentMonth = day.Month == CurrentMonthDate.Month;
                var hasEvents = EventsForMonth.Count(e => e.Date.Date == day.Date) > 0;
                var isToday = day.Date == DateTime.Today.Date;
                var isSelected = day.Date == SelectedDate.Date;

                calendarDays.Add(new CalendarDayViewModel
                {
                    Date = day,
                    IsCurrentMonth = isCurrentMonth,
                    HasEvents = hasEvents,
                    IsToday = isToday,
                    IsSelected = isSelected
                });
            }

            CalendarDays = calendarDays;
        }

        private async Task MarkEventCompletedAsync(CalendarEvent calendarEvent)
        {
            if (calendarEvent == null) return;

            try
            {
                IsBusy = true;

                // Toggle completion state
                bool newState = !calendarEvent.IsCompleted;
                
                // Update the event through the service
                await _calendarService.MarkEventAsCompletedAsync(calendarEvent.Id, newState);
                
                // Update the local model state
                calendarEvent.IsCompleted = newState;
                
                // Refresh data
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Failed to update event: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void HandleError(string message, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"{message}: {ex.Message}");
            HasError = true;
            ErrorMessage = $"{message}. Please try again.";
        }
    }
} 