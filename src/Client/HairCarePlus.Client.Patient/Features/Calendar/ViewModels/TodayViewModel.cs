using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using Microsoft.Maui.Controls;
using System.Diagnostics;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    public class TodayViewModel : BaseViewModel
    {
        private readonly ICalendarService _calendarService;
        private DateTime _selectedDate;
        private ObservableCollection<DateTime> _calendarDays;
        private ObservableCollection<GroupedCalendarEvents> _todayEvents;
        private ObservableCollection<CalendarEvent> _flattenedEvents;
        private string _daysSinceTransplant;
        private Dictionary<DateTime, Dictionary<EventType, int>> _eventCountsByDate;
        private int _overdueEventsCount;
        
        // Ключ для хранения выбранной даты в локальном хранилище
        private const string SelectedDateKey = "LastSelectedDate";
        
        public TodayViewModel(ICalendarService calendarService)
        {
            _calendarService = calendarService;
            
            // Восстанавливаем сохраненную дату или используем сегодняшний день
            _selectedDate = LoadLastSelectedDate() ?? DateTime.Today;
            
            _eventCountsByDate = new Dictionary<DateTime, Dictionary<EventType, int>>();
            _overdueEventsCount = 0;
            Title = "Today";
            
            // Initialize commands
            ToggleEventCompletionCommand = new Command<CalendarEvent>(async (calendarEvent) => await ToggleEventCompletionAsync(calendarEvent));
            SelectDateCommand = new Command<DateTime>(async (date) => await SelectDateAsync(date));
            OpenMonthCalendarCommand = new Command<DateTime>(async (date) => await OpenMonthCalendarAsync(date));
            ViewEventDetailsCommand = new Command<CalendarEvent>(async (calendarEvent) => await ViewEventDetailsAsync(calendarEvent));
            PostponeEventCommand = new Command<CalendarEvent>(async (calendarEvent) => await PostponeEventAsync(calendarEvent));
            
            // Initial data loading
            LoadCalendarDays();
            Task.Run(async () => 
            {
                await LoadTodayEventsAsync();
                await LoadEventCountsForVisibleDaysAsync();
                await CheckOverdueEventsAsync();
            });
        }
        
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                {
                    Debug.WriteLine($"SelectedDate changed to: {value.ToShortDateString()}");
                    Task.Run(async () => 
                    {
                        await LoadTodayEventsAsync();
                        // Сохраняем выбранную дату при каждом изменении
                        SaveSelectedDate(value);
                    });
                    OnPropertyChanged(nameof(FormattedSelectedDate));
                    OnPropertyChanged(nameof(DaysSinceTransplant));
                }
            }
        }
        
        public string FormattedSelectedDate => SelectedDate.ToString("ddd, MMM d");
        
        public string FormattedTodayDate => DateTime.Today.ToString("ddd, MMM d");
        
        public string DaysSinceTransplant
        {
            get => _daysSinceTransplant;
            set => SetProperty(ref _daysSinceTransplant, value);
        }
        
        public ObservableCollection<DateTime> CalendarDays
        {
            get => _calendarDays;
            set => SetProperty(ref _calendarDays, value);
        }
        
        public ObservableCollection<GroupedCalendarEvents> TodayEvents
        {
            get => _todayEvents;
            set => SetProperty(ref _todayEvents, value);
        }
        
        public ObservableCollection<CalendarEvent> FlattenedEvents
        {
            get => _flattenedEvents;
            set => SetProperty(ref _flattenedEvents, value);
        }
        
        public Dictionary<DateTime, Dictionary<EventType, int>> EventCountsByDate
        {
            get => _eventCountsByDate;
            private set => SetProperty(ref _eventCountsByDate, value);
        }
        
        public int OverdueEventsCount
        {
            get => _overdueEventsCount;
            set => SetProperty(ref _overdueEventsCount, value);
        }
        
        public ICommand ToggleEventCompletionCommand { get; }
        public ICommand SelectDateCommand { get; }
        public ICommand OpenMonthCalendarCommand { get; }
        public ICommand ViewEventDetailsCommand { get; }
        public ICommand PostponeEventCommand { get; }
        
        private void LoadCalendarDays()
        {
            // Generate a range of dates centered around the selected date
            // Include -7 to +30 days to show some past dates as well
            var startDate = DateTime.Today.AddDays(-7);
            var days = Enumerable.Range(0, 38)
                .Select(offset => startDate.AddDays(offset))
                .ToList();
                
            CalendarDays = new ObservableCollection<DateTime>(days);
            
            // Calculate days since transplant based on a hardcoded transplant date
            // In a real app, this would come from the patient's profile or medical record
            var transplantDate = DateTime.Today.AddDays(-1); // Surgery was yesterday
            var daysSince = (int)(DateTime.Today - transplantDate).TotalDays;
            DaysSinceTransplant = $"Day {daysSince} post hair transplant";
        }
        
        public async Task LoadTodayEventsAsync()
        {
            try
            {
                // Load events for the selected date
                var events = await _calendarService.GetEventsForDateAsync(SelectedDate);
                
                // Update UI on the main thread
                if (Application.Current?.MainPage?.Dispatcher != null)
                {
                    // Use proper way to dispatch async actions in MAUI
                    await Application.Current.MainPage.Dispatcher.DispatchAsync(() => 
                    {
                        FlattenedEvents = new ObservableCollection<CalendarEvent>(events);
                        OnPropertyChanged(nameof(FlattenedEvents));
                        return Task.CompletedTask;
                    });
                }
                else
                {
                    FlattenedEvents = new ObservableCollection<CalendarEvent>(events);
                    OnPropertyChanged(nameof(FlattenedEvents));
                }
                
                // Log loaded events for debugging
                Debug.WriteLine($"LoadTodayEventsAsync: Loaded {events.Count()} events for {SelectedDate:yyyy-MM-dd}");
                foreach (var evt in events)
                {
                    Debug.WriteLine($"Event: {evt.Title}, Type: {evt.EventType}, Time: {evt.Date:HH:mm}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in LoadTodayEventsAsync: {ex.Message}");
                if (Application.Current?.MainPage?.Dispatcher != null)
                {
                    // Use proper way to dispatch async actions in MAUI
                    await Application.Current.MainPage.Dispatcher.DispatchAsync(() => 
                    {
                        FlattenedEvents = new ObservableCollection<CalendarEvent>();
                        return Task.CompletedTask;
                    });
                }
                else
                {
                    FlattenedEvents = new ObservableCollection<CalendarEvent>();
                }
            }
        }
        
        public async Task LoadEventCountsForVisibleDaysAsync()
        {
            if (CalendarDays == null || !CalendarDays.Any())
                return;

            var startDate = CalendarDays.First();
            var endDate = CalendarDays.Last();
            
            var allEvents = await _calendarService.GetEventsForDateRangeAsync(startDate, endDate);
            var result = new Dictionary<DateTime, Dictionary<EventType, int>>();
            
            // Initialize dictionary for all days
            foreach (var day in CalendarDays)
            {
                result[day.Date] = new Dictionary<EventType, int>
                {
                    { EventType.MedicationTreatment, 0 },
                    { EventType.Photo, 0 },
                    { EventType.CriticalWarning, 0 },
                    { EventType.VideoInstruction, 0 },
                    { EventType.MedicalVisit, 0 },
                    { EventType.GeneralRecommendation, 0 }
                };
            }
            
            // Count events for each day and type
            foreach (var evt in allEvents)
            {
                if (evt.IsMultiDay)
                {
                    // Handle multi-day events
                    var currentDate = evt.Date.Date;
                    while (currentDate <= evt.EndDate.Value.Date)
                    {
                        if (result.ContainsKey(currentDate))
                        {
                            result[currentDate][evt.EventType]++;
                        }
                        currentDate = currentDate.AddDays(1);
                    }
                }
                else
                {
                    // Handle single-day events
                    if (result.ContainsKey(evt.Date.Date))
                    {
                        result[evt.Date.Date][evt.EventType]++;
                    }
                }
            }
            
            EventCountsByDate = result;
            OnPropertyChanged(nameof(EventCountsByDate));
        }
        
        public async Task CheckOverdueEventsAsync()
        {
            try
            {
                var overdueEvents = await _calendarService.GetOverdueEventsAsync();
                OverdueEventsCount = overdueEvents.Count();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking overdue events: {ex.Message}");
                OverdueEventsCount = 0;
            }
        }
        
        // Проверяет, является ли событие просроченным
        public bool IsEventOverdue(CalendarEvent evt)
        {
            if (evt == null)
                return false;
                
            // Событие считается просроченным, если:
            // 1. Оно не выполнено
            // 2. Дата события уже прошла
            return !evt.IsCompleted && evt.Date.Date < DateTime.Today;
        }
        
        public bool HasEvents(DateTime date)
        {
            if (EventCountsByDate != null && EventCountsByDate.TryGetValue(date.Date, out var counts))
            {
                return counts.Values.Sum() > 0;
            }
            return false;
        }
        
        public int GetEventCount(DateTime date, EventType eventType)
        {
            if (EventCountsByDate != null && EventCountsByDate.TryGetValue(date.Date, out var counts))
            {
                if (counts.TryGetValue(eventType, out var count))
                {
                    return count;
                }
            }
            return 0;
        }
        
        public async Task ToggleEventCompletionAsync(CalendarEvent calendarEvent)
        {
            if (calendarEvent != null)
            {
                calendarEvent.IsCompleted = !calendarEvent.IsCompleted;
                await _calendarService.MarkEventAsCompletedAsync(calendarEvent.Id, calendarEvent.IsCompleted);
                
                // Перезагружаем события для обновления и правильной сортировки
                await LoadTodayEventsAsync();
                
                // Обновляем счетчик просроченных событий, если событие было просроченным
                if (calendarEvent.Date.Date < DateTime.Today)
                {
                    await CheckOverdueEventsAsync();
                }
            }
        }
        
        private async Task SelectDateAsync(DateTime date)
        {
            Debug.WriteLine($"SelectDateAsync called with date: {date.ToShortDateString()}");
            Debug.WriteLine($"Current SelectedDate before change: {SelectedDate.ToShortDateString()}");
            
            SelectedDate = date;
            
            Debug.WriteLine($"SelectedDate after change: {SelectedDate.ToShortDateString()}");
            Debug.WriteLine($"Loading events for date: {date.ToShortDateString()}");
            
            // Reload events for the selected date
            await LoadTodayEventsAsync();
            
            Debug.WriteLine($"Events loaded: {FlattenedEvents?.Count ?? 0} events found");
        }
        
        private async Task OpenMonthCalendarAsync(DateTime date)
        {
            // Сохраняем выбранную дату перед переходом
            SelectedDate = date;
            
            // В реальном приложении здесь был бы код для навигации к полному месячному календарю
            // Например, с использованием Shell.Current.GoToAsync или INavigationService
            
            // Пример перехода к месячному календарю (заглушка для демонстрации):
            await Shell.Current.GoToAsync("//calendar/month?date=" + date.ToString("yyyy-MM-dd"));
        }
        
        // Переход к детальной странице события
        private async Task ViewEventDetailsAsync(CalendarEvent calendarEvent)
        {
            if (calendarEvent == null)
                return;
                
            // В реальном приложении здесь был бы код для навигации к детальной странице события
            // Например, с использованием Shell.Current.GoToAsync или INavigationService
            
            // Пример перехода к детальной странице (заглушка для демонстрации):
            await Shell.Current.GoToAsync($"//calendar/event?id={calendarEvent.Id}");
        }
        
        // Отложить событие на более позднее время
        private async Task PostponeEventAsync(CalendarEvent calendarEvent)
        {
            if (calendarEvent == null)
                return;
                
            // В реальном приложении здесь был бы код для открытия диалога выбора нового времени
            // и сохранения измененного события
            
            // Пример отложения события на день (заглушка для демонстрации):
            await Application.Current.MainPage.DisplayActionSheet(
                "Postpone Event", 
                "Cancel", 
                null,
                "Postpone 1 hour", 
                "Postpone to this evening", 
                "Postpone to tomorrow", 
                "Postpone to next week");
                
            // После выбора нового времени, обновили бы событие и перезагрузили список
            // await LoadTodayEventsAsync();
        }
        
        // Сохранение выбранной даты в локальное хранилище
        private void SaveSelectedDate(DateTime date)
        {
            try
            {
                Preferences.Set(SelectedDateKey, date.ToString("o"));
            }
            catch (Exception ex)
            {
                // В реальном приложении здесь был бы код логирования ошибки
                Console.WriteLine($"Error saving selected date: {ex.Message}");
            }
        }
        
        // Загрузка последней выбранной даты из локального хранилища
        private DateTime? LoadLastSelectedDate()
        {
            try
            {
                string savedDateString = Preferences.Get(SelectedDateKey, null);
                if (!string.IsNullOrEmpty(savedDateString) && DateTime.TryParse(savedDateString, out DateTime savedDate))
                {
                    return savedDate;
                }
            }
            catch (Exception ex)
            {
                // В реальном приложении здесь был бы код логирования ошибки
                Console.WriteLine($"Error loading selected date: {ex.Message}");
            }
            
            return null;
        }
    }
    
    // Класс для группировки событий по времени суток
    public class GroupedCalendarEvents : ObservableCollection<CalendarEvent>
    {
        public string Name { get; private set; }
        
        public GroupedCalendarEvents(string name, IEnumerable<CalendarEvent> events) : base(events)
        {
            Name = name;
        }
    }
} 