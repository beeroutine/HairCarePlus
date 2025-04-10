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
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    /// <summary>
    /// A timer to display and track active restrictions
    /// </summary>
    public class RestrictionTimerViewModel : ObservableObject
    {
        private CalendarEvent? _restrictionEvent;
        private string _remainingTimeText = string.Empty;
        private double _progressPercentage;

        public CalendarEvent? RestrictionEvent
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

        public string? Title => RestrictionEvent?.Title;
        public string? Description => RestrictionEvent?.Description;
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
        private string _errorMessage = string.Empty;

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
            _ = LoadEventsForSelectedDateAsync();
        }

        partial void OnSelectedDateChanged(DateTime value)
        {
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

        partial void OnEventsForMonthChanged(ObservableCollection<CalendarEvent> value)
        {
            MainThread.BeginInvokeOnMainThread(() => {
                try {
                    UpdateCalendarDays();
                } catch (Exception ex) {
                    HandleError("Error updating calendar days", ex);
                }
            });
        }

        private void InitializeCommands()
        {
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            GoToTodayCommand = new RelayCommand(ExecuteGoToToday);
            NextMonthCommand = new RelayCommand(ExecuteNextMonth);
            PreviousMonthCommand = new RelayCommand(ExecutePreviousMonth);
            MarkEventCompletedCommand = new RelayCommand<CalendarEvent>(ExecuteMarkEventCompleted);
            DaySelectedCommand = new RelayCommand<DateTime>(ExecuteDaySelected);
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
            
            try
            {
                IsBusy = true;
                HasError = false;
                ErrorMessage = string.Empty;

                await LoadEventsForMonthAsync(CurrentMonthDate.Year, CurrentMonthDate.Month);
                await LoadEventsForSelectedDateAsync();
                await LoadActiveRestrictionsAsync();
                UpdateCalendarDays();
            }
            catch (Exception ex)
            {
                HandleError("Error refreshing calendar", ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadEventsForMonthAsync(int year, int month)
        {
            if (IsBusy) return;
            
            try
            {
                IsBusy = true;
                HasError = false;

                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var events = await _eventAggregationService.GetEventsForDateRangeAsync(startDate, endDate);
                EventsForMonth = new ObservableCollection<CalendarEvent>(events);
            }
            catch (Exception ex)
            {
                HandleError("Error loading events for month", ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadEventsForSelectedDateAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                HasError = false;

                var events = await _eventAggregationService.GetEventsForDateAsync(SelectedDate);
                EventsForSelectedDate = new ObservableCollection<CalendarEvent>(events);
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
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                HasError = false;

                var restrictions = await _eventAggregationService.GetActiveRestrictionsAsync();
                ActiveRestrictions = new ObservableCollection<RestrictionTimerViewModel>(
                    restrictions.Select(r => new RestrictionTimerViewModel { RestrictionEvent = r }));
            }
            catch (Exception ex)
            {
                HandleError("Error loading active restrictions", ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateCalendarDays()
        {
            var firstDayOfMonth = new DateTime(CurrentMonthDate.Year, CurrentMonthDate.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var days = new List<CalendarDayViewModel>();

            // Add days from previous month to fill first week
            var firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
            var previousMonth = firstDayOfMonth.AddMonths(-1);
            var daysInPreviousMonth = DateTime.DaysInMonth(previousMonth.Year, previousMonth.Month);

            for (int i = firstDayOfWeek - 1; i >= 0; i--)
            {
                var date = new DateTime(previousMonth.Year, previousMonth.Month, daysInPreviousMonth - i);
                var events = EventsForMonth.Where(e => e.Date.Date == date.Date).ToList();
                days.Add(new CalendarDayViewModel
                {
                    Date = date,
                    IsCurrentMonth = false,
                    IsToday = date.Date == DateTime.Today.Date,
                    IsSelected = date.Date == SelectedDate.Date,
                    Events = new ObservableCollection<CalendarEvent>(events)
                });
            }

            // Add days of current month
            for (int i = 1; i <= lastDayOfMonth.Day; i++)
            {
                var date = new DateTime(CurrentMonthDate.Year, CurrentMonthDate.Month, i);
                var events = EventsForMonth.Where(e => e.Date.Date == date.Date).ToList();
                days.Add(new CalendarDayViewModel
                {
                    Date = date,
                    IsCurrentMonth = true,
                    IsToday = date.Date == DateTime.Today.Date,
                    IsSelected = date.Date == SelectedDate.Date,
                    Events = new ObservableCollection<CalendarEvent>(events)
                });
            }

            // Add days from next month to complete the last week
            var lastDayOfWeek = (int)lastDayOfMonth.DayOfWeek;
            var nextMonth = lastDayOfMonth.AddDays(1);
            for (int i = 1; i <= 6 - lastDayOfWeek; i++)
            {
                var date = new DateTime(nextMonth.Year, nextMonth.Month, i);
                var events = EventsForMonth.Where(e => e.Date.Date == date.Date).ToList();
                days.Add(new CalendarDayViewModel
                {
                    Date = date,
                    IsCurrentMonth = false,
                    IsToday = date.Date == DateTime.Today.Date,
                    IsSelected = date.Date == SelectedDate.Date,
                    Events = new ObservableCollection<CalendarEvent>(events)
                });
            }

            CalendarDays = new ObservableCollection<CalendarDayViewModel>(days);
        }

        private async Task MarkEventCompletedAsync(CalendarEvent calendarEvent)
        {
            if (IsBusy || calendarEvent == null) return;

            try
            {
                IsBusy = true;
                HasError = false;

                await _calendarService.MarkEventAsCompletedAsync(calendarEvent.Id);
                await _notificationService.ShowSuccessAsync("Event marked as completed");
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                HandleError("Error marking event as completed", ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void HandleError(string message, Exception ex)
        {
            HasError = true;
            ErrorMessage = $"{message}: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"ERROR: {message}");
            System.Diagnostics.Debug.WriteLine($"Exception: {ex}");
        }
    }
} 