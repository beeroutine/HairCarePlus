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
using System.Threading;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    public class TodayViewModel : BaseViewModel
    {
        private readonly ICalendarService _calendarService;
        private readonly ILogger<TodayViewModel> _logger;
        private DateTime _selectedDate;
        private ObservableCollection<DateTime> _calendarDays;
        private ObservableCollection<GroupedCalendarEvents> _todayEvents;
        private ObservableCollection<CalendarEvent> _flattenedEvents;
        private ObservableCollection<CalendarEvent> _sortedEvents;
        private string _daysSinceTransplant;
        private Dictionary<DateTime, Dictionary<EventType, int>> _eventCountsByDate;
        private int _overdueEventsCount;
        private double _completionProgress;
        private int _completionPercentage;
        private bool _isLoadingMore;
        private const int InitialDaysToLoad = 90; // Changed from 38 to 90 days (3 months)
        private const int DaysToLoadMore = 60; // Changed from 30 to 60 days (2 months)
        private const int MaxTotalDays = 365; // New constant to prevent memory issues
        private DateTime _lastLoadedDate;

        // New fields for enhanced functionality
        private readonly Dictionary<DateTime, List<CalendarEvent>> _eventCache;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(1);
        private CancellationTokenSource? _loadingCancellationSource;
        private int _loadingProgress;
        private string _loadingStatus;
        private const int MaxRetryAttempts = 3;
        private const int BatchSize = 10;

        public enum LoadingState
        {
            NotStarted,
            Loading,
            Completed,
            Error
        }

        private LoadingState _loadingState;
        
        public LoadingState CurrentLoadingState
        {
            get => _loadingState;
            private set
            {
                if (_loadingState != value)
                {
                    _loadingState = value;
                    OnPropertyChanged(nameof(CurrentLoadingState));
                    OnPropertyChanged(nameof(IsLoading));
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        public bool IsLoading => CurrentLoadingState == LoadingState.Loading;
        public bool HasError => CurrentLoadingState == LoadingState.Error;

        public int LoadingProgress
        {
            get => _loadingProgress;
            private set
            {
                if (_loadingProgress != value)
                {
                    _loadingProgress = value;
                    OnPropertyChanged(nameof(LoadingProgress));
                }
            }
        }

        public string LoadingStatus
        {
            get => _loadingStatus;
            private set
            {
                if (_loadingStatus != value)
                {
                    _loadingStatus = value;
                    OnPropertyChanged(nameof(LoadingStatus));
                }
            }
        }

        // Keys for local storage
        private const string SelectedDateKey = "LastSelectedDate";
        
        public TodayViewModel(ICalendarService calendarService, ILogger<TodayViewModel> logger)
        {
            _calendarService = calendarService;
            _logger = logger;
            
            // Restore saved date or use today
            _selectedDate = LoadLastSelectedDate() ?? DateTime.Today;
            _lastLoadedDate = DateTime.Today.AddDays(30); // Initial last loaded date
            
            _eventCountsByDate = new Dictionary<DateTime, Dictionary<EventType, int>>();
            _eventCache = new Dictionary<DateTime, List<CalendarEvent>>();
            _overdueEventsCount = 0;
            _completionProgress = 0;
            _completionPercentage = 0;
            _isLoadingMore = false;
            _loadingProgress = 0;
            _loadingStatus = string.Empty;
            _loadingState = LoadingState.NotStarted;
            Title = "Today";
            
            // Initialize commands
            ToggleEventCompletionCommand = new Command<CalendarEvent>(async (calendarEvent) => await ToggleEventCompletionAsync(calendarEvent));
            SelectDateCommand = new Command<DateTime>(async (date) => await SelectDateAsync(date));
            OpenMonthCalendarCommand = new Command<DateTime>(async (date) => await OpenMonthCalendarAsync(date));
            ViewEventDetailsCommand = new Command<CalendarEvent>(async (calendarEvent) => await ViewEventDetailsAsync(calendarEvent));
            PostponeEventCommand = new Command<CalendarEvent>(async (calendarEvent) => await PostponeEventAsync(calendarEvent));
            ShowEventDetailsCommand = new Command<CalendarEvent>(async (calendarEvent) => await ShowEventDetailsAsync(calendarEvent));
            LoadMoreDatesCommand = new Command(async () => await LoadMoreDatesAsync(), () => !IsLoading);
            
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
                    OnPropertyChanged(nameof(CurrentMonthName));
                }
            }
        }
        
        public string FormattedSelectedDate => SelectedDate.ToString("ddd, MMM d");
        
        public string FormattedTodayDate => DateTime.Today.ToString("ddd, MMM d");
        
        public string CurrentMonthName => SelectedDate.ToString("MMMM");
        
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
        
        public ObservableCollection<CalendarEvent> SortedEvents
        {
            get => _sortedEvents;
            set => SetProperty(ref _sortedEvents, value);
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
        
        public double CompletionProgress
        {
            get => _completionProgress;
            set => SetProperty(ref _completionProgress, value);
        }
        
        public int CompletionPercentage
        {
            get => _completionPercentage;
            set => SetProperty(ref _completionPercentage, value);
        }
        
        public ICommand ToggleEventCompletionCommand { get; }
        public ICommand SelectDateCommand { get; }
        public ICommand OpenMonthCalendarCommand { get; }
        public ICommand ViewEventDetailsCommand { get; }
        public ICommand PostponeEventCommand { get; }
        public ICommand ShowEventDetailsCommand { get; }
        public ICommand LoadMoreDatesCommand { get; }
        
        private void LoadCalendarDays()
        {
            try
            {
                _logger.LogInformation("Starting LoadCalendarDays");
                
                // Calculate the start date to be 7 days before today
                var startDate = DateTime.Today.AddDays(-7);
                _logger.LogInformation($"Start date: {startDate:yyyy-MM-dd}");

                // Generate dates for the initial range
                var days = new List<DateTime>();
                for (int i = 0; i < InitialDaysToLoad; i++)
                {
                    days.Add(startDate.AddDays(i));
                }
                
                CalendarDays = new ObservableCollection<DateTime>(days);
                _lastLoadedDate = days.Last();
                
                _logger.LogInformation($"Loaded {days.Count} days from {days.First():yyyy-MM-dd} to {days.Last():yyyy-MM-dd}");

                // Calculate days since transplant
                var transplantDate = DateTime.Today.AddDays(-1);
                var daysSince = (int)(DateTime.Today - transplantDate).TotalDays;
                DaysSinceTransplant = $"Day {daysSince} post hair transplant";
                
                _logger.LogInformation("LoadCalendarDays completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LoadCalendarDays");
                // Initialize with empty collection in case of error
                CalendarDays = new ObservableCollection<DateTime>();
            }
        }
        
        private async Task LoadMoreDatesAsync()
        {
            if (IsLoading) return;

            try
            {
                _logger.LogInformation("Starting LoadMoreDatesAsync");
                
                // Check if we've reached the maximum allowed days
                var totalDays = (CalendarDays.Last() - CalendarDays.First()).TotalDays;
                _logger.LogInformation($"Current total days: {totalDays}");
                
                if (totalDays >= MaxTotalDays)
                {
                    _logger.LogInformation("Maximum calendar range reached");
                    LoadingStatus = "Maximum calendar range reached";
                    return;
                }

                // Cancel any existing loading operation
                _loadingCancellationSource?.Cancel();
                _loadingCancellationSource = new CancellationTokenSource();
                var cancellationToken = _loadingCancellationSource.Token;

                CurrentLoadingState = LoadingState.Loading;
                LoadingProgress = 0;
                LoadingStatus = "Preparing to load more dates...";

                // Calculate new date range
                var startDate = _lastLoadedDate.AddDays(1);
                var endDate = startDate.AddDays(DaysToLoadMore - 1);
                _logger.LogInformation($"Loading dates from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

                var totalDaysToLoad = (endDate - startDate).Days + 1;
                var processedDays = 0;

                // Generate new dates
                var newDates = new List<DateTime>();
                for (int i = 0; i < DaysToLoadMore; i++)
                {
                    newDates.Add(startDate.AddDays(i));
                }

                // Process dates in batches
                for (int i = 0; i < newDates.Count; i += BatchSize)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException();
                    }

                    var batchDates = newDates.Skip(i).Take(BatchSize).ToList();
                    LoadingStatus = $"Loading events for {batchDates[0]:MMM dd} - {batchDates[^1]:MMM dd}...";

                    await LoadEventsForBatchAsync(batchDates, cancellationToken);

                    processedDays += batchDates.Count;
                    LoadingProgress = (int)((double)processedDays / totalDaysToLoad * 100);

                    // Add new dates to collection on the main thread
                    await Application.Current.MainPage.Dispatcher.DispatchAsync(() =>
                    {
                        foreach (var date in batchDates)
                        {
                            CalendarDays.Add(date);
                        }
                    });
                }

                // Update last loaded date
                _lastLoadedDate = endDate;
                CurrentLoadingState = LoadingState.Completed;
                LoadingStatus = "Loading completed successfully";
                _logger.LogInformation($"Successfully loaded {processedDays} new dates");

                // Cleanup old cache entries
                CleanupCache();
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Loading cancelled");
                LoadingStatus = "Loading cancelled";
                CurrentLoadingState = LoadingState.NotStarted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LoadMoreDatesAsync");
                LoadingStatus = "Error loading dates. Tap to retry.";
                CurrentLoadingState = LoadingState.Error;
            }
            finally
            {
                _loadingCancellationSource?.Dispose();
                _loadingCancellationSource = null;
            }
        }
        
        private async Task LoadEventsForBatchAsync(List<DateTime> dates, CancellationToken cancellationToken)
        {
            foreach (var date in dates)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }

                // Check cache first
                if (_eventCache.TryGetValue(date, out var cachedEvents))
                {
                    var cacheAge = DateTime.Now - date;
                    if (cacheAge <= _cacheExpiration)
                    {
                        // Use cached data
                        _logger.LogInformation("Cache hit for date {Date}. Events count: {Count}", date.ToShortDateString(), cachedEvents.Count);
                        UpdateEventCountsForDate(date, cachedEvents);
                        continue;
                    }
                    else
                    {
                        _logger.LogInformation("Cache expired for date {Date}. Age: {Age}", date.ToShortDateString(), cacheAge);
                    }
                }
                else
                {
                    _logger.LogInformation("Cache miss for date {Date}", date.ToShortDateString());
                }

                // Load from service with retry
                var events = await LoadEventsWithRetryAsync(date, cancellationToken);
                
                // Update cache and counts
                _eventCache[date] = events;
                _logger.LogInformation("Cached {Count} events for date {Date}", events.Count, date.ToShortDateString());
                UpdateEventCountsForDate(date, events);
            }
        }
        
        private async Task<List<CalendarEvent>> LoadEventsWithRetryAsync(DateTime date, CancellationToken cancellationToken)
        {
            int retryCount = 0;
            while (retryCount < MaxRetryAttempts)
            {
                try
                {
                    var events = await _calendarService.GetEventsForDateAsync(date);
                    if (retryCount > 0)
                    {
                        _logger.LogInformation("Successfully loaded events for {Date} after {RetryCount} retries", date.ToShortDateString(), retryCount);
                    }
                    return events?.ToList() ?? new List<CalendarEvent>();
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogWarning(ex, "Error loading events for {Date}. Retry {RetryCount}/{MaxRetries}", date.ToShortDateString(), retryCount, MaxRetryAttempts);
                    
                    if (retryCount >= MaxRetryAttempts)
                    {
                        _logger.LogError(ex, "Failed to load events for {Date} after {MaxRetries} retries", date.ToShortDateString(), MaxRetryAttempts);
                        throw;
                    }

                    // Exponential backoff
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                    await Task.Delay(delay, cancellationToken);
                }
            }

            return new List<CalendarEvent>();
        }
        
        private void UpdateEventCountsForDate(DateTime date, List<CalendarEvent> events)
        {
            if (!_eventCountsByDate.ContainsKey(date))
            {
                _eventCountsByDate[date] = new Dictionary<EventType, int>();
            }

            foreach (var eventType in Enum.GetValues<EventType>())
            {
                _eventCountsByDate[date][eventType] = events.Count(e => e.EventType == eventType);
            }
        }
        
        private void CleanupCache()
        {
            var oldestAllowedDate = DateTime.Today.AddDays(-30); // Keep last 30 days
            var keysToRemove = _eventCache.Keys.Where(date => date < oldestAllowedDate).ToList();
            
            if (keysToRemove.Any())
            {
                _logger.LogInformation("Cleaning up cache. Removing {Count} entries older than {Date}", 
                    keysToRemove.Count, oldestAllowedDate.ToShortDateString());
                
                foreach (var key in keysToRemove)
                {
                    _eventCache.Remove(key);
                    _eventCountsByDate.Remove(key);
                }
            }
        }
        
        public async Task LoadTodayEventsAsync()
        {
            try
            {
                _logger.LogInformation($"Loading events for date: {SelectedDate:yyyy-MM-dd}");
                
                // Load events for the selected date
                var events = await _calendarService.GetEventsForDateAsync(SelectedDate);
                
                // Update UI on the main thread
                await Application.Current.MainPage.Dispatcher.DispatchAsync(() => 
                {
                    FlattenedEvents = new ObservableCollection<CalendarEvent>(events ?? new List<CalendarEvent>());
                    SortedEvents = new ObservableCollection<CalendarEvent>(
                        (events ?? new List<CalendarEvent>())
                        .OrderBy(e => e.Date.TimeOfDay)
                        .ToList());
                    UpdateCompletionProgress();
                    OnPropertyChanged(nameof(FlattenedEvents));
                    OnPropertyChanged(nameof(SortedEvents));
                });
                
                // Log loaded events
                _logger.LogInformation($"Loaded {events?.Count() ?? 0} events for {SelectedDate:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading events for date {SelectedDate:yyyy-MM-dd}");
                await Application.Current.MainPage.Dispatcher.DispatchAsync(() => 
                {
                    FlattenedEvents = new ObservableCollection<CalendarEvent>();
                    SortedEvents = new ObservableCollection<CalendarEvent>();
                    UpdateCompletionProgress();
                    OnPropertyChanged(nameof(FlattenedEvents));
                    OnPropertyChanged(nameof(SortedEvents));
                });
            }
        }
        
        private void UpdateCompletionProgress()
        {
            var events = FlattenedEvents;
            if (events == null || events.Count == 0)
            {
                CompletionProgress = 0;
                CompletionPercentage = 0;
                return;
            }
            
            int totalEvents = events.Count;
            int completedEvents = events.Count(e => e.IsCompleted);
            
            CompletionProgress = (double)completedEvents / totalEvents;
            CompletionPercentage = (int)(CompletionProgress * 100);
        }
        
        // Метод для показа деталей события
        private async Task ShowEventDetailsAsync(CalendarEvent calendarEvent)
        {
            if (calendarEvent == null)
                return;
                
            // Пример отображения полных деталей события
            await Application.Current.MainPage.DisplayAlert(
                calendarEvent.Title,
                calendarEvent.Description,
                "OK");
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
                
                // Обновляем прогресс выполнения
                UpdateCompletionProgress();
                
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
            
            if (SelectedDate.Date != date.Date)
            {
                SelectedDate = date;
                // Явно вызываем уведомление об изменении, чтобы UI обновился
                OnPropertyChanged(nameof(SelectedDate));
                
                Debug.WriteLine($"SelectedDate after change: {SelectedDate.ToShortDateString()}");
                Debug.WriteLine($"Loading events for date: {date.ToShortDateString()}");
                
                // Reload events for the selected date
                await LoadTodayEventsAsync();
                
                Debug.WriteLine($"Events loaded: {FlattenedEvents?.Count ?? 0} events found");
            }
        }
        
        private async Task OpenMonthCalendarAsync(DateTime date)
        {
            // Сохраняем выбранную дату перед переходом
            SelectedDate = date;
            
            // Вместо навигации к несуществующей странице покажем сообщение
            await Application.Current.MainPage.DisplayAlert(
                "Календарь", 
                $"Полный календарь для даты {date:dd.MM.yyyy} находится в разработке",
                "OK");
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