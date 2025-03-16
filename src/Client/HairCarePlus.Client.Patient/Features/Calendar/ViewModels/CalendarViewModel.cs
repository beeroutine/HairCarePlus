using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    public class CalendarViewModel : BaseViewModel
    {
        private readonly ICalendarService _calendarService;
        private readonly INotificationService _notificationService;
        
        private DateTime _selectedDate = DateTime.Today;
        private DateTime _currentMonthDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        private ObservableCollection<CalendarEvent> _eventsForSelectedDate = new ObservableCollection<CalendarEvent>();
        private ObservableCollection<CalendarEvent> _activeRestrictions = new ObservableCollection<CalendarEvent>();

        public ICommand RefreshCommand { get; private set; }
        public ICommand GoToTodayCommand { get; private set; }
        public ICommand NextMonthCommand { get; private set; }
        public ICommand PreviousMonthCommand { get; private set; }
        public ICommand MarkEventCompletedCommand { get; private set; }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                {
                    LoadEventsForSelectedDateAsync().ConfigureAwait(false);
                }
            }
        }

        public DateTime CurrentMonthDate
        {
            get => _currentMonthDate;
            set
            {
                if (SetProperty(ref _currentMonthDate, value))
                {
                    LoadEventsForMonthAsync().ConfigureAwait(false);
                }
            }
        }

        public ObservableCollection<CalendarEvent> EventsForSelectedDate
        {
            get => _eventsForSelectedDate;
            set => SetProperty(ref _eventsForSelectedDate, value);
        }

        public ObservableCollection<CalendarEvent> ActiveRestrictions
        {
            get => _activeRestrictions;
            set => SetProperty(ref _activeRestrictions, value);
        }

        /// <summary>
        /// Default constructor for XAML
        /// </summary>
        public CalendarViewModel() 
        {
            Title = "Calendar";
            
            // Note: When created through XAML, services will be null
            // This constructor is primarily for design-time and preview
            
            // Initialize commands with no-op implementations
            RefreshCommand = new Command(() => { });
            GoToTodayCommand = new Command(() => { });
            NextMonthCommand = new Command(() => { });
            PreviousMonthCommand = new Command(() => { });
            MarkEventCompletedCommand = new Command<CalendarEvent>(_ => { });
        }

        public CalendarViewModel(ICalendarService calendarService, INotificationService notificationService)
        {
            Title = "Calendar";
            _calendarService = calendarService ?? throw new ArgumentNullException(nameof(calendarService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

            RefreshCommand = new Command(async () => await RefreshDataAsync());
            GoToTodayCommand = new Command(ExecuteGoToToday);
            NextMonthCommand = new Command(ExecuteNextMonth);
            PreviousMonthCommand = new Command(ExecutePreviousMonth);
            MarkEventCompletedCommand = new Command<CalendarEvent>(ExecuteMarkEventCompleted);

            // Initial loading of data
            Task.Run(async () =>
            {
                await RefreshDataAsync();
            });
        }

        private async Task RefreshDataAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                await LoadEventsForSelectedDateAsync();
                await LoadEventsForMonthAsync();
                await LoadActiveRestrictionsAsync();
            }
            catch (Exception ex)
            {
                // Log error or show message to user
                System.Diagnostics.Debug.WriteLine($"Error refreshing calendar data: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ExecuteGoToToday()
        {
            SelectedDate = DateTime.Today;
            CurrentMonthDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        }

        private void ExecuteNextMonth()
        {
            CurrentMonthDate = CurrentMonthDate.AddMonths(1);
        }

        private void ExecutePreviousMonth()
        {
            CurrentMonthDate = CurrentMonthDate.AddMonths(-1);
        }

        private async void ExecuteMarkEventCompleted(CalendarEvent calendarEvent)
        {
            if (calendarEvent == null)
                return;

            try
            {
                IsBusy = true;
                await _calendarService.MarkEventAsCompletedAsync(calendarEvent.Id, !calendarEvent.IsCompleted);
                
                // Update the local item's completion status
                calendarEvent.IsCompleted = !calendarEvent.IsCompleted;
                
                // Refresh the events list
                await LoadEventsForSelectedDateAsync();
            }
            catch (Exception)
            {
                // In a real app, show an error message to the user
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
                var events = await _calendarService.GetEventsForDateAsync(SelectedDate);
                
                EventsForSelectedDate.Clear();
                foreach (var evt in events.OrderBy(e => e.Date))
                {
                    EventsForSelectedDate.Add(evt);
                }
            }
            catch (Exception)
            {
                // In a real app, show an error message to the user
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadEventsForMonthAsync()
        {
            try
            {
                IsBusy = true;
                
                // Get the first and last day of the month
                var firstDayOfMonth = new DateTime(CurrentMonthDate.Year, CurrentMonthDate.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
                
                // We also need to include the days from previous/next month that appear in the calendar view
                var firstDayOfCalendarView = firstDayOfMonth.AddDays(-(int)firstDayOfMonth.DayOfWeek);
                var lastDayOfCalendarView = lastDayOfMonth.AddDays(6 - (int)lastDayOfMonth.DayOfWeek);
                
                // Get events for the entire calendar view
                await _calendarService.GetEventsForDateRangeAsync(firstDayOfCalendarView, lastDayOfCalendarView);
                
                // In a real app, we'd update a property here to display events in the calendar
            }
            catch (Exception)
            {
                // In a real app, show an error message to the user
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
                IsBusy = true;
                var restrictions = await _calendarService.GetActiveRestrictionsAsync();
                
                ActiveRestrictions.Clear();
                foreach (var restriction in restrictions)
                {
                    ActiveRestrictions.Add(restriction);
                }
            }
            catch (Exception)
            {
                // In a real app, show an error message to the user
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
} 