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
    public class CalendarDayViewModel : BaseViewModel
    {
        private DateTime _date;
        private bool _hasEvents;
        private bool _isSelected;
        private bool _isToday;

        public DateTime Date 
        { 
            get => _date; 
            set => SetProperty(ref _date, value); 
        }
        
        public bool HasEvents 
        { 
            get => _hasEvents; 
            set => SetProperty(ref _hasEvents, value); 
        }
        
        public bool IsSelected 
        { 
            get => _isSelected; 
            set => SetProperty(ref _isSelected, value); 
        }
        
        public bool IsToday 
        { 
            get => _isToday; 
            set => SetProperty(ref _isToday, value); 
        }
        
        public string DayOfMonth => Date.Day.ToString();
        public string DayOfWeek => Date.ToString("ddd");
    }

    public class HorizontalCalendarViewModel : BaseViewModel
    {
        private readonly ICalendarService _calendarService;
        private ObservableCollection<CalendarDayViewModel> _days = new ObservableCollection<CalendarDayViewModel>();
        private ObservableCollection<CalendarEvent> _selectedDayEvents = new ObservableCollection<CalendarEvent>();
        private DateTime _startDate;
        private CalendarDayViewModel _selectedDay;

        public ObservableCollection<CalendarDayViewModel> Days
        {
            get => _days;
            set => SetProperty(ref _days, value);
        }

        public ObservableCollection<CalendarEvent> SelectedDayEvents
        {
            get => _selectedDayEvents;
            set => SetProperty(ref _selectedDayEvents, value);
        }

        public CalendarDayViewModel SelectedDay
        {
            get => _selectedDay;
            set
            {
                if (_selectedDay != null)
                    _selectedDay.IsSelected = false;
                
                SetProperty(ref _selectedDay, value);
                
                if (_selectedDay != null)
                {
                    _selectedDay.IsSelected = true;
                    LoadEventsForSelectedDay();
                }
            }
        }

        public ICommand NextWeekCommand { get; }
        public ICommand PreviousWeekCommand { get; }
        public ICommand SelectDayCommand { get; }
        public ICommand GoToTodayCommand { get; }

        public HorizontalCalendarViewModel(ICalendarService calendarService)
        {
            Title = "Week Calendar";
            _calendarService = calendarService ?? throw new ArgumentNullException(nameof(calendarService));
            
            NextWeekCommand = new Command(ExecuteNextWeek);
            PreviousWeekCommand = new Command(ExecutePreviousWeek);
            SelectDayCommand = new Command<CalendarDayViewModel>(ExecuteSelectDay);
            GoToTodayCommand = new Command(ExecuteGoToToday);

            // Start with today at the center
            _startDate = DateTime.Today.AddDays(-3);
            
            // Initialize with a 7-day view
            GenerateDays();
            
            // Select today by default
            SelectedDay = Days.FirstOrDefault(d => d.Date.Date == DateTime.Today.Date);
        }

        private void GenerateDays()
        {
            Days.Clear();
            
            for (int i = 0; i < 7; i++)
            {
                var date = _startDate.AddDays(i);
                Days.Add(new CalendarDayViewModel
                {
                    Date = date,
                    IsToday = date.Date == DateTime.Today.Date,
                    IsSelected = SelectedDay?.Date.Date == date.Date
                });
            }
            
            // Load events indicators
            LoadEventsForDays();
        }

        private async void LoadEventsForDays()
        {
            try
            {
                IsBusy = true;
                
                var events = await _calendarService.GetEventsForDateRangeAsync(_startDate, _startDate.AddDays(6));
                
                // Group events by date
                var eventsByDate = events.GroupBy(e => e.Date.Date);
                
                // Update HasEvents property for each day
                foreach (var day in Days)
                {
                    day.HasEvents = eventsByDate.Any(g => g.Key == day.Date.Date);
                }
            }
            catch (Exception)
            {
                // In a real app, handle the error appropriately
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void LoadEventsForSelectedDay()
        {
            if (SelectedDay == null)
                return;
                
            try
            {
                IsBusy = true;
                
                var events = await _calendarService.GetEventsForDateAsync(SelectedDay.Date);
                
                SelectedDayEvents.Clear();
                foreach (var evt in events.OrderBy(e => e.Date))
                {
                    SelectedDayEvents.Add(evt);
                }
            }
            catch (Exception)
            {
                // In a real app, handle the error appropriately
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ExecuteNextWeek()
        {
            _startDate = _startDate.AddDays(7);
            GenerateDays();
        }

        private void ExecutePreviousWeek()
        {
            _startDate = _startDate.AddDays(-7);
            GenerateDays();
        }

        private void ExecuteSelectDay(CalendarDayViewModel day)
        {
            if (day != null)
                SelectedDay = day;
        }

        private void ExecuteGoToToday()
        {
            _startDate = DateTime.Today.AddDays(-3);
            GenerateDays();
            SelectedDay = Days.FirstOrDefault(d => d.Date.Date == DateTime.Today.Date);
        }
    }
} 