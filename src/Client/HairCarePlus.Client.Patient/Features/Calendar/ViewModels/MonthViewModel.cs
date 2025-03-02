using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using System;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    public class CalendarDayViewModel : ObservableObject
    {
        private DateTime _date;
        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        private bool _isCurrentMonth;
        public bool IsCurrentMonth
        {
            get => _isCurrentMonth;
            set => SetProperty(ref _isCurrentMonth, value);
        }

        private bool _isToday;
        public bool IsToday
        {
            get => _isToday;
            set => SetProperty(ref _isToday, value);
        }

        private ObservableCollection<CalendarEventViewModel> _events = new ObservableCollection<CalendarEventViewModel>();
        public ObservableCollection<CalendarEventViewModel> Events
        {
            get => _events;
            set => SetProperty(ref _events, value);
        }

        // Properties for event type indicators
        private bool _hasMedications = false;
        public bool HasMedications
        {
            get => _hasMedications;
            set => SetProperty(ref _hasMedications, value);
        }

        private bool _hasInstructions = false;
        public bool HasInstructions
        {
            get => _hasInstructions;
            set => SetProperty(ref _hasInstructions, value);
        }

        private bool _hasRestrictions = false;
        public bool HasRestrictions
        {
            get => _hasRestrictions;
            set => SetProperty(ref _hasRestrictions, value);
        }
    }

    public class MonthViewModel : ObservableObject
    {
        private readonly ICalendarService _calendarService;
        private bool _hasMedications = false;
        private bool _hasInstructions = false;
        private bool _hasRestrictions = false;

        private DateTime _selectedDate;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set => SetProperty(ref _selectedDate, value);
        }

        private ObservableCollection<CalendarDayViewModel> _days = new ObservableCollection<CalendarDayViewModel>();
        public ObservableCollection<CalendarDayViewModel> Days
        {
            get => _days;
            set => SetProperty(ref _days, value);
        }

        private string _monthTitle;
        public string MonthTitle
        {
            get => _monthTitle;
            set => SetProperty(ref _monthTitle, value);
        }

        private CalendarDayViewModel _selectedDay;
        public CalendarDayViewModel SelectedDay
        {
            get => _selectedDay;
            set => SetProperty(ref _selectedDay, value);
        }

        public IRelayCommand PreviousMonthCommand { get; }
        public IRelayCommand NextMonthCommand { get; }
        public IAsyncRelayCommand<CalendarDayViewModel> DaySelectedCommand { get; }

        public MonthViewModel(ICalendarService calendarService)
        {
            _calendarService = calendarService;
            
            PreviousMonthCommand = new RelayCommand(PreviousMonth);
            NextMonthCommand = new RelayCommand(NextMonth);
            DaySelectedCommand = new AsyncRelayCommand<CalendarDayViewModel>(DaySelected);
            
            LoadMonth(DateTime.Now);
        }

        private void PreviousMonth()
        {
            LoadMonth(SelectedDate.AddMonths(-1));
        }

        private void NextMonth()
        {
            LoadMonth(SelectedDate.AddMonths(1));
        }

        private async Task DaySelected(CalendarDayViewModel day)
        {
            if (day == null)
                return;

            SelectedDay = day;

            if (day.Events.Count > 0)
            {
                // Show event details with an alert
                var eventTypes = new System.Collections.Generic.List<string>();
                if (day.HasMedications) eventTypes.Add("Medications");
                if (day.HasInstructions) eventTypes.Add("Instructions");
                if (day.HasRestrictions) eventTypes.Add("Restrictions");

                await Application.Current.MainPage.DisplayAlert(
                    $"Events for {day.Date:dd/MM/yyyy}",
                    $"You have {string.Join(", ", eventTypes)} scheduled for this day.",
                    "OK");
            }
        }

        private void LoadMonth(DateTime date)
        {
            SelectedDate = new DateTime(date.Year, date.Month, 1);
            MonthTitle = SelectedDate.ToString("MMMM yyyy");

            Days.Clear();

            // Add days from the previous month to start on the correct weekday
            var firstDayOfMonth = SelectedDate;
            var daysFromPreviousMonth = ((int)firstDayOfMonth.DayOfWeek + 6) % 7; // Adjust for Monday as first day of week
            
            if (daysFromPreviousMonth > 0)
            {
                var previousMonth = firstDayOfMonth.AddMonths(-1);
                var daysInPreviousMonth = DateTime.DaysInMonth(previousMonth.Year, previousMonth.Month);
                
                for (int i = daysInPreviousMonth - daysFromPreviousMonth + 1; i <= daysInPreviousMonth; i++)
                {
                    var day = new DateTime(previousMonth.Year, previousMonth.Month, i);
                    var events = new ObservableCollection<CalendarEventViewModel>();

                    Days.Add(new CalendarDayViewModel
                    {
                        Date = day,
                        IsCurrentMonth = false,
                        IsToday = day.Date == DateTime.Today,
                        Events = events
                    });
                }
            }

            // Add days from current month
            var daysInMonth = DateTime.DaysInMonth(SelectedDate.Year, SelectedDate.Month);
            for (int i = 1; i <= daysInMonth; i++)
            {
                var day = new DateTime(SelectedDate.Year, SelectedDate.Month, i);
                var events = new ObservableCollection<CalendarEventViewModel>();
                _hasMedications = false;
                _hasInstructions = false;
                _hasRestrictions = false;

                // Add some dummy events based on the day number
                // In a real app, these would come from a database or API
                if (i % 3 == 0) // Every 3rd day has a medication
                {
                    events.Add(new CalendarEventViewModel { Name = "Принять лекарство", Type = "Medication", Date = day });
                    _hasMedications = true;
                }

                if (i % 5 == 0) // Every 5th day has instructions
                {
                    events.Add(new CalendarEventViewModel { Name = "Уход за швами", Type = "Instruction", Date = day });
                    _hasInstructions = true;
                }

                if (i % 7 == 0) // Every 7th day has restrictions
                {
                    events.Add(new CalendarEventViewModel { Name = "Без физ. нагрузки", Type = "Restriction", Date = day });
                    _hasRestrictions = true;
                }

                Days.Add(new CalendarDayViewModel
                {
                    Date = day,
                    IsCurrentMonth = true,
                    IsToday = day.Date == DateTime.Today,
                    Events = events,
                    HasMedications = _hasMedications,
                    HasInstructions = _hasInstructions,
                    HasRestrictions = _hasRestrictions
                });
            }

            // Add days from the next month to complete the grid
            var lastDayOfMonth = new DateTime(SelectedDate.Year, SelectedDate.Month, daysInMonth);
            var daysFromNextMonth = 42 - Days.Count; // 42 cells for a standard calendar (6 rows x 7 columns)
            
            for (int i = 1; i <= daysFromNextMonth; i++)
            {
                var day = new DateTime(lastDayOfMonth.Year, lastDayOfMonth.Month, 1).AddMonths(1).AddDays(i - 1);
                var events = new ObservableCollection<CalendarEventViewModel>();

                Days.Add(new CalendarDayViewModel
                {
                    Date = day,
                    IsCurrentMonth = false,
                    IsToday = day.Date == DateTime.Today,
                    Events = events
                });
            }
        }
    }
} 