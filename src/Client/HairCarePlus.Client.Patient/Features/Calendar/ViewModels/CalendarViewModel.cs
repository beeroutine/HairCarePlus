using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using NotificationsService = HairCarePlus.Client.Patient.Features.Notifications.Services.Interfaces;
using RestrictTimer = HairCarePlus.Client.Patient.Features.Calendar.ViewModels.RestrictTimerItem;
using INotificationsService = HairCarePlus.Client.Patient.Features.Notifications.Services.Interfaces.INotificationService;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    public partial class CalendarViewModel : ObservableObject
    {
        public enum CalendarViewMode
        {
            Month,
            Day
        }

        private readonly ICalendarService _calendarService;
        private readonly INotificationsService _notificationService;
        private readonly IEventAggregationService _eventAggregationService;

        // Keep track of the last loaded month to avoid reloading unnecessarily
        private DateTime _lastLoadedMonth;

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
        private ObservableCollection<CalendarEvent> _morningEvents = new();

        [ObservableProperty]
        private ObservableCollection<CalendarEvent> _afternoonEvents = new();

        [ObservableProperty]
        private ObservableCollection<CalendarEvent> _eveningEvents = new();

        [ObservableProperty]
        private ObservableCollection<RestrictTimer> _activeRestrictions = new();

        [ObservableProperty]
        private ObservableCollection<CalendarDayViewModel> _calendarDays = new();

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private int _retryCount;

        [ObservableProperty]
        private bool _isLoaded = false;

        [ObservableProperty]
        private CalendarViewMode _viewMode = CalendarViewMode.Month;

        private const int MaxRetries = 3;

        // Commands - initialize them in the constructor
        public ICommand RefreshCommand { get; }
        public ICommand GoToTodayCommand { get; }
        public ICommand NextMonthCommand { get; }
        public ICommand PreviousMonthCommand { get; }
        public ICommand MarkEventCompletedCommand { get; }
        public ICommand DaySelectedCommand { get; }
        public ICommand SwitchViewModeCommand { get; }

        public CalendarViewModel(
            ICalendarService calendarService,
            INotificationsService notificationService,
            IEventAggregationService eventAggregationService)
        {
            _calendarService = calendarService;
            _notificationService = notificationService;
            _eventAggregationService = eventAggregationService;
            _lastLoadedMonth = CurrentMonthDate;

            // Initialize commands
            RefreshCommand = new Command(async () => await RefreshAsync());
            GoToTodayCommand = new Command(ExecuteGoToToday);
            NextMonthCommand = new Command(ExecuteNextMonth);
            PreviousMonthCommand = new Command(ExecutePreviousMonth);
            MarkEventCompletedCommand = new Command<CalendarEvent>(ExecuteMarkEventCompleted);
            DaySelectedCommand = new Command<DateTime>(ExecuteDaySelected);
            SwitchViewModeCommand = new Command<CalendarViewMode>(ExecuteSwitchViewMode);
            
            // Initialize
            UpdateCalendarDays();
            
            // Load events for the current month and selected date when first opening
            Task.Run(async () => 
            {
                await LoadEventsForMonthAsync(CurrentMonthDate.Year, CurrentMonthDate.Month);
                await LoadEventsForSelectedDateAsync();
                IsLoaded = true;
            });
        }

        private void ExecuteSwitchViewMode(CalendarViewMode mode)
        {
            ViewMode = mode;
        }

        partial void OnSelectedDateChanged(DateTime value)
        {
            LoadEventsForSelectedDateAsync().ConfigureAwait(false);
            UpdateCalendarDays();
        }

        partial void OnDisplayMonthChanged(DateTime value)
        {
            // Check if we've already loaded this month to prevent unnecessary API calls
            if (_lastLoadedMonth.Year != value.Year || _lastLoadedMonth.Month != value.Month)
            {
                LoadEventsForMonthAsync(value.Year, value.Month).ConfigureAwait(false);
                _lastLoadedMonth = value;
            }
        }

        partial void OnCurrentMonthDateChanged(DateTime value)
        {
            // Check if we've already loaded this month to prevent unnecessary API calls
            if (_lastLoadedMonth.Year != value.Year || _lastLoadedMonth.Month != value.Month)
            {
                LoadEventsForMonthAsync(value.Year, value.Month).ConfigureAwait(false);
                _lastLoadedMonth = value;
            }
            UpdateCalendarDays();
        }

        partial void OnEventsForMonthChanged(ObservableCollection<CalendarEvent> value)
        {
            // When events for the month change, update the calendar days to show event indicators
            UpdateCalendarDays();
        }

        partial void OnEventsForSelectedDateChanged(ObservableCollection<CalendarEvent> value)
        {
            // Сортируем события по времени суток
            GroupEventsByTimeOfDay();
        }

        private void GroupEventsByTimeOfDay()
        {
            MorningEvents.Clear();
            AfternoonEvents.Clear();
            EveningEvents.Clear();

            foreach (var calendarEvent in EventsForSelectedDate)
            {
                switch (calendarEvent.TimeOfDay)
                {
                    case TimeOfDay.Morning:
                        MorningEvents.Add(calendarEvent);
                        break;
                    case TimeOfDay.Afternoon:
                        AfternoonEvents.Add(calendarEvent);
                        break;
                    case TimeOfDay.Evening:
                        EveningEvents.Add(calendarEvent);
                        break;
                }
            }
        }

        private void ExecuteGoToToday()
        {
            CurrentMonthDate = DateTime.Today;
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
            SelectedDate = date;
            ViewMode = CalendarViewMode.Day;
        }

        private void ExecuteMarkEventCompleted(CalendarEvent calendarEvent)
        {
            if (calendarEvent != null)
            {
                MarkEventCompletedAsync(calendarEvent).ConfigureAwait(false);
            }
        }

        private async Task RefreshAsync()
        {
            try
            {
                IsBusy = true;
                HasError = false;
                
                // Refresh events for the current month
                await LoadEventsForMonthAsync(CurrentMonthDate.Year, CurrentMonthDate.Month, forceRefresh: true);
                
                // Refresh events for the selected date
                await LoadEventsForSelectedDateAsync(forceRefresh: true);
                
                // Refresh active restrictions
                await LoadActiveRestrictionsAsync();
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = "Could not refresh calendar data. Please try again.";
                System.Diagnostics.Debug.WriteLine($"Error in RefreshAsync: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadEventsForSelectedDateAsync(bool forceRefresh = false)
        {
            if (IsBusy && !forceRefresh) return;
            
            try
            {
                IsBusy = true;
                HasError = false;

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

                // Update the observable collection
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    EventsForSelectedDate.Clear();
                    foreach (var evnt in allDayEvents)
                    {
                        EventsForSelectedDate.Add(evnt);
                    }
                });
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = "Could not load events for this date. Please try again.";
                System.Diagnostics.Debug.WriteLine($"Error in LoadEventsForSelectedDateAsync: {ex.Message}");
                
                // Retry logic
                if (RetryCount < MaxRetries)
                {
                    RetryCount++;
                    await Task.Delay(500 * RetryCount); // Exponential backoff
                    await LoadEventsForSelectedDateAsync(forceRefresh);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadEventsForMonthAsync(int year, int month, bool forceRefresh = false)
        {
            if (IsBusy && !forceRefresh) return;
            
            try
            {
                IsBusy = true;
                HasError = false;
                
                // Get events for the month
                var events = await _calendarService.GetEventsForMonthAsync(year, month);
                if (events != null)
                {
                    // Update the observable collection
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        EventsForMonth.Clear();
                        foreach (var evnt in events)
                        {
                            EventsForMonth.Add(evnt);
                        }
                    });
                }
                
                // Also load active restrictions
                await LoadActiveRestrictionsAsync();
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = "Could not load calendar events. Please try again.";
                System.Diagnostics.Debug.WriteLine($"Error in LoadEventsForMonthAsync: {ex.Message}");
                
                // Retry logic
                if (RetryCount < MaxRetries)
                {
                    RetryCount++;
                    await Task.Delay(500 * RetryCount); // Exponential backoff
                    await LoadEventsForMonthAsync(year, month, forceRefresh);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadActiveRestrictionsAsync()
        {
            if (_calendarService == null) return;
            
            try
            {
                // Get active restrictions
                var restrictions = await _calendarService.GetActiveRestrictionsAsync();
                if (restrictions != null)
                {
                    // Create timers for active restrictions
                    var activeRestrictionTimers = new List<RestrictTimer>();
                    foreach (var restriction in restrictions.Where(r => r.EventType == EventType.Restriction))
                    {
                        // Only add restrictions that haven't expired
                        if (restriction.ExpirationDate.HasValue && restriction.ExpirationDate.Value > DateTime.Now)
                        {
                            var timer = new RestrictTimerItem
                            {
                                RestrictionEvent = restriction,
                                EndDate = restriction.ExpirationDate.Value
                            };
                            activeRestrictionTimers.Add(timer);
                        }
                    }
                    
                    // Update the observable collection
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ActiveRestrictions.Clear();
                        foreach (var timer in activeRestrictionTimers)
                        {
                            ActiveRestrictions.Add(timer);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoadActiveRestrictionsAsync: {ex.Message}");
                // Don't set HasError here to avoid showing error for restrictions only
            }
        }

        private void UpdateCalendarDays()
        {
            try
            {
                var firstDayOfMonth = new DateTime(CurrentMonthDate.Year, CurrentMonthDate.Month, 1);
                var firstDayOfCalendarView = firstDayOfMonth.AddDays(-(int)firstDayOfMonth.DayOfWeek);

                var calendarDays = new ObservableCollection<CalendarDayViewModel>();
                for (int i = 0; i < 42; i++) // 6 weeks x 7 days
                {
                    var day = firstDayOfCalendarView.AddDays(i);
                    var isCurrentMonth = day.Month == CurrentMonthDate.Month;
                    var isToday = day.Date == DateTime.Today.Date;
                    var isSelected = day.Date == SelectedDate.Date;

                    // Находим события для этого дня
                    var eventsForDay = EventsForMonth.Where(e => e.Date.Date == day.Date).ToList();
                    var hasEvents = eventsForDay.Any();
                    
                    var calendarDayVM = new CalendarDayViewModel
                    {
                        Date = day,
                        IsCurrentMonth = isCurrentMonth,
                        HasEvents = hasEvents,
                        IsToday = isToday,
                        IsSelected = isSelected,
                        TotalEvents = eventsForDay.Count
                    };

                    // Устанавливаем наличие различных типов событий
                    if (hasEvents)
                    {
                        calendarDayVM.HasMedication = eventsForDay.Any(e => e.EventType == EventType.Medication);
                        calendarDayVM.HasPhoto = eventsForDay.Any(e => e.EventType == EventType.Photo);
                        calendarDayVM.HasRestriction = eventsForDay.Any(e => e.EventType == EventType.Restriction);
                        calendarDayVM.HasInstruction = eventsForDay.Any(e => e.EventType == EventType.Instruction);
                    }

                    calendarDays.Add(calendarDayVM);
                }

                CalendarDays = calendarDays;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateCalendarDays: {ex.Message}");
            }
        }

        private async Task MarkEventCompletedAsync(CalendarEvent calendarEvent)
        {
            if (calendarEvent == null) return;
            
            try
            {
                IsBusy = true;
                
                // Toggle completion status
                calendarEvent.IsCompleted = !calendarEvent.IsCompleted;
                
                // Update on server
                await _calendarService.MarkEventAsCompletedAsync(calendarEvent.Id, calendarEvent.IsCompleted);
                
                // If this is a restriction that just got completed, refresh the active restrictions
                if (calendarEvent.EventType == EventType.Restriction)
                {
                    await LoadActiveRestrictionsAsync();
                }
                
                // Refresh the events for the selected date
                await LoadEventsForSelectedDateAsync(forceRefresh: true);
            }
            catch (Exception ex)
            {
                // Revert the status change on error
                calendarEvent.IsCompleted = !calendarEvent.IsCompleted;
                
                HasError = true;
                ErrorMessage = "Could not update event status. Please try again.";
                System.Diagnostics.Debug.WriteLine($"Error in MarkEventCompletedAsync: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
} 