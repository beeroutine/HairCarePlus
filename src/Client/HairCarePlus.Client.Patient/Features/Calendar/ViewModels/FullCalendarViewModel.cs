using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using Microsoft.Maui.Controls;
using INotificationsService = HairCarePlus.Client.Patient.Features.Notifications.Services.Interfaces.INotificationService;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    public class AdaptiveDayViewModel : BaseViewModel
    {
        private DateTime _date;
        private bool _isCurrentMonth;
        private bool _isToday;
        private bool _isSelected;
        private ObservableCollection<CalendarEvent> _events = new();

        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        public bool IsCurrentMonth
        {
            get => _isCurrentMonth;
            set => SetProperty(ref _isCurrentMonth, value);
        }

        public bool IsToday
        {
            get => _isToday;
            set => SetProperty(ref _isToday, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public ObservableCollection<CalendarEvent> Events
        {
            get => _events;
            set => SetProperty(ref _events, value);
        }

        public int Day => Date.Day;
        
        public bool HasEvents => Events.Count > 0;
        
        public bool HasMedicationEvents => Events.Any(e => e.EventType == EventType.Medication);
        
        public bool HasPhotoEvents => Events.Any(e => e.EventType == EventType.Photo);
        
        public bool HasRestrictionEvents => Events.Any(e => e.EventType == EventType.Restriction);
        
        public bool HasInstructionEvents => Events.Any(e => e.EventType == EventType.Instruction);
        
        public bool HasExcessEvents => Events.Count > 3;
        
        public string ExcessEventsText => $"+{Events.Count - 3}";
    }

    public class DayEventsGroup : BaseViewModel
    {
        private TimeOfDay _timeOfDay;
        private ObservableCollection<CalendarEvent> _events = new();

        public TimeOfDay TimeOfDay
        {
            get => _timeOfDay;
            set => SetProperty(ref _timeOfDay, value);
        }

        public ObservableCollection<CalendarEvent> Events
        {
            get => _events;
            set => SetProperty(ref _events, value);
        }

        public string Header
        {
            get
            {
                return TimeOfDay switch
                {
                    TimeOfDay.Morning => "🌅 Утро",
                    TimeOfDay.Afternoon => "☀️ День",
                    TimeOfDay.Evening => "🌙 Вечер",
                    _ => string.Empty
                };
            }
        }
        
        public bool HasEvents => Events.Count > 0;
    }

    public class FullCalendarViewModel : BaseViewModel
    {
        private readonly ICalendarService _calendarService;
        private readonly INotificationsService _notificationService;
        private readonly IEventAggregationService _eventAggregationService;

        private DateTime _currentMonthDate;
        private DateTime _selectedDate;
        private bool _isMonthViewVisible = true;
        private ObservableCollection<AdaptiveDayViewModel> _days = new();
        private ObservableCollection<DayEventsGroup> _dayEventsGroups = new();
        private bool _hasEvents;

        public DateTime CurrentMonthDate
        {
            get => _currentMonthDate;
            set
            {
                if (SetProperty(ref _currentMonthDate, value))
                {
                    LoadMonthEventsAsync().ConfigureAwait(false);
                }
            }
        }

        public string CurrentMonthYear => CurrentMonthDate.ToString("MMMM yyyy");

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                {
                    LoadDayEventsAsync().ConfigureAwait(false);
                }
            }
        }

        public string SelectedDateText => SelectedDate.ToString("d MMMM");

        public bool IsMonthViewVisible
        {
            get => _isMonthViewVisible;
            set => SetProperty(ref _isMonthViewVisible, value);
        }

        public ObservableCollection<AdaptiveDayViewModel> Days
        {
            get => _days;
            set => SetProperty(ref _days, value);
        }

        public ObservableCollection<DayEventsGroup> DayEventsGroups
        {
            get => _dayEventsGroups;
            set => SetProperty(ref _dayEventsGroups, value);
        }
        
        public bool HasEvents
        {
            get => _hasEvents;
            set => SetProperty(ref _hasEvents, value);
        }

        public ICommand PreviousMonthCommand { get; }
        public ICommand NextMonthCommand { get; }
        public ICommand DaySelectedCommand { get; }
        public ICommand GoToTodayCommand { get; }
        public ICommand BackToMonthViewCommand { get; }
        public ICommand MarkEventCompletedCommand { get; }
        public ICommand RefreshCommand { get; }

        public FullCalendarViewModel(
            ICalendarService calendarService,
            INotificationsService notificationService,
            IEventAggregationService eventAggregationService)
        {
            Title = "Календарь";
            _calendarService = calendarService ?? throw new ArgumentNullException(nameof(calendarService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _eventAggregationService = eventAggregationService ?? throw new ArgumentNullException(nameof(eventAggregationService));

            CurrentMonthDate = DateTime.Today;
            SelectedDate = DateTime.Today;

            PreviousMonthCommand = new Command(ExecutePreviousMonth);
            NextMonthCommand = new Command(ExecuteNextMonth);
            DaySelectedCommand = new Command<DateTime>(ExecuteDaySelected);
            GoToTodayCommand = new Command(ExecuteGoToToday);
            BackToMonthViewCommand = new Command(ExecuteBackToMonthView);
            MarkEventCompletedCommand = new Command<CalendarEvent>(MarkEventCompleted);
            RefreshCommand = new Command(async () => await RefreshCalendarAsync());

            // Инициализируем группы событий по времени суток
            DayEventsGroups.Add(new DayEventsGroup { TimeOfDay = TimeOfDay.Morning });
            DayEventsGroups.Add(new DayEventsGroup { TimeOfDay = TimeOfDay.Afternoon });
            DayEventsGroups.Add(new DayEventsGroup { TimeOfDay = TimeOfDay.Evening });

            Task.Run(RefreshCalendarAsync);
        }

        private void ExecutePreviousMonth()
        {
            CurrentMonthDate = CurrentMonthDate.AddMonths(-1);
        }

        private void ExecuteNextMonth()
        {
            CurrentMonthDate = CurrentMonthDate.AddMonths(1);
        }

        private void ExecuteDaySelected(DateTime date)
        {
            SelectedDate = date;
            IsMonthViewVisible = false;
        }

        private void ExecuteGoToToday()
        {
            CurrentMonthDate = DateTime.Today;
            SelectedDate = DateTime.Today;
        }

        private void ExecuteBackToMonthView()
        {
            IsMonthViewVisible = true;
        }

        private async void MarkEventCompleted(CalendarEvent calendarEvent)
        {
            if (calendarEvent == null) return;

            try
            {
                calendarEvent.IsCompleted = !calendarEvent.IsCompleted;
                await _calendarService.MarkEventAsCompletedAsync(calendarEvent.Id, calendarEvent.IsCompleted);
                
                // Обновляем UI
                OnPropertyChanged(nameof(DayEventsGroups));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error marking event as completed: {ex}");
            }
        }

        private async Task RefreshCalendarAsync()
        {
            try
            {
                IsBusy = true;
                await LoadMonthEventsAsync();
                await LoadDayEventsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing calendar: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadMonthEventsAsync()
        {
            try
            {
                // Очищаем предыдущую коллекцию
                Days.Clear();
                
                // Получаем первый день месяца
                var firstDayOfMonth = new DateTime(CurrentMonthDate.Year, CurrentMonthDate.Month, 1);
                
                // Получаем первый день в календарной сетке (может быть из предыдущего месяца)
                var firstDayOfCalendar = firstDayOfMonth.AddDays(-(int)firstDayOfMonth.DayOfWeek);
                
                // Получаем события для месяца
                var events = await _calendarService.GetEventsForMonthAsync(CurrentMonthDate.Year, CurrentMonthDate.Month);
                
                // Создаем дни календаря на весь период отображения (42 дня - 6 недель)
                var currentDate = firstDayOfCalendar;
                for (int i = 0; i < 42; i++)
                {
                    var day = new AdaptiveDayViewModel
                    {
                        Date = currentDate,
                        IsCurrentMonth = currentDate.Month == CurrentMonthDate.Month,
                        IsToday = currentDate.Date == DateTime.Today,
                        IsSelected = currentDate.Date == SelectedDate.Date,
                        Events = new ObservableCollection<CalendarEvent>(
                            events.Where(e => e.Date.Date == currentDate.Date).ToList())
                    };
                    
                    Days.Add(day);
                    currentDate = currentDate.AddDays(1);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading month events: {ex}");
            }
        }

        private async Task LoadDayEventsAsync()
        {
            try
            {
                // Получаем события для выбранного дня
                var events = await _calendarService.GetEventsForDateAsync(SelectedDate);
                
                // Группируем события по времени суток
                var morningEvents = events.Where(e => e.TimeOfDay == TimeOfDay.Morning).ToList();
                var afternoonEvents = events.Where(e => e.TimeOfDay == TimeOfDay.Afternoon).ToList();
                var eveningEvents = events.Where(e => e.TimeOfDay == TimeOfDay.Evening).ToList();
                
                // Обновляем группы
                DayEventsGroups[0].Events = new ObservableCollection<CalendarEvent>(morningEvents);
                DayEventsGroups[1].Events = new ObservableCollection<CalendarEvent>(afternoonEvents);
                DayEventsGroups[2].Events = new ObservableCollection<CalendarEvent>(eveningEvents);
                
                // Обновляем флаг наличия событий
                HasEvents = events.Any();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading day events: {ex}");
            }
        }
    }
} 