using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using Microsoft.Maui.Controls;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Text;
using CommunityToolkit.Mvvm.Messaging;
using HairCarePlus.Client.Patient.Features.Calendar.Messages;
using Microsoft.Maui.Graphics;
using HairCarePlus.Client.Patient.Features.Calendar.Services;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    public class TodayViewModel : BaseViewModel
    {
        private readonly ICalendarService _calendarService;
        private readonly ICalendarCacheService _cacheService;
        private readonly ICalendarLoader _eventLoader;
        private readonly IProgressCalculator _progressCalculator;
        private readonly ILogger<TodayViewModel> _logger;
        private DateTime _selectedDate;
        private ObservableCollection<DateTime> _calendarDays;
        private ObservableCollection<GroupedCalendarEvents> _todayEvents;
        private ObservableCollection<CalendarEvent> _flattenedEvents;
        private ObservableCollection<CalendarEvent> _sortedEvents;
        private Dictionary<DateTime, Dictionary<EventType, int>> _eventCountsByDate;
        private int _overdueEventsCount;
        private double _completionProgress;
        private int _completionPercentage;
        private bool _isLoadingMore;
        private const int InitialDaysToLoad = 365; // Было 90, теперь 365 дней (1 год)
        private const int DaysToLoadMore = 60; // Changed from 30 to 60 days (2 months)
        private const int MaxTotalDays = 365; // New constant to prevent memory issues
        private DateTime _lastLoadedDate;

        // New fields for enhanced functionality
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(1);
        private CancellationTokenSource? _loadingCancellationSource;
        private int _loadingProgress;
        private string _loadingStatus;
        private const int MaxRetryAttempts = 3;
        private const int BatchSize = 10;
        private const int RefreshTimeoutMilliseconds = 30000; // 30 seconds timeout
        // Предопределенные интервалы для retry (в миллисекундах): 1s, 2s, 4s
        private static readonly int[] RetryDelays = { 1000, 2000, 4000 };
        private SemaphoreSlim _refreshSemaphore = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _refreshCancellationTokenSource;

        private bool _isRefreshing;
        private const string SelectedDateKey = "LastSelectedDate";
        
        private DateTime _lastRefreshTime = DateTime.MinValue;
        private readonly TimeSpan _throttleInterval = TimeSpan.FromMilliseconds(300); // Минимальный интервал между запросами
        
        // Диагностические счетчики для тестирования эффективности
        private static int _totalRequests = 0;
        private static int _cacheHits = 0;
        private static int _cacheMisses = 0;
        private static int _throttledRequests = 0;
        private static int _concurrentRejections = 0;
        
        // Диагностические счетчики для дополнительных метрик
        private int _eventCountsRequests = 0;
        private int _eventCountsCacheHits = 0;
        private int _eventCountsBatchRequests = 0;
        
        private bool _isDataLoaded;
        private bool _isRefreshingData;
        
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        public ICommand RefreshCommand { get; private set; }
        public ICommand GoToTodayCommand { get; private set; }

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
                    OnPropertyChanged(nameof(IsContentVisible));
                }
            }
        }

        public bool HasError => CurrentLoadingState == LoadingState.Error;
        public bool IsContentVisible => CurrentLoadingState != LoadingState.Loading && CurrentLoadingState != LoadingState.Error;

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

        // New property to signal scroll target
        private DateTime? _scrollToIndexTarget;
        public DateTime? ScrollToIndexTarget
        {
            get => _scrollToIndexTarget;
            set => SetProperty(ref _scrollToIndexTarget, value);
        }

        public TodayViewModel(ICalendarService calendarService, ICalendarCacheService cacheService, ICalendarLoader eventLoader, IProgressCalculator progressCalculator, ILogger<TodayViewModel> logger)
        {
            _calendarService = calendarService;
            _cacheService = cacheService;
            _eventLoader = eventLoader;
            _progressCalculator = progressCalculator;
            _logger = logger;
            
            // Restore saved date or use today
            _selectedDate = LoadLastSelectedDate() ?? DateTime.Today;
            _lastLoadedDate = DateTime.Today.AddDays(30); // Initial last loaded date
            
            _eventCountsByDate = new Dictionary<DateTime, Dictionary<EventType, int>>();
            _overdueEventsCount = 0;
            _completionProgress = 0;
            _completionPercentage = 0;
            _isLoadingMore = false;
            _loadingProgress = 0;
            _loadingStatus = string.Empty;
            _loadingState = LoadingState.NotStarted;
            Title = "Today";
            
            // Initialize commands
            RefreshCommand = new Command(async () => await RefreshDataAsync());
            ToggleEventCompletionCommand = new Command<CalendarEvent>(async (calendarEvent) => await ToggleEventCompletionAsync(calendarEvent));
            SelectDateCommand = new Command<DateTime>(async (date) => await SelectDateAsync(date));
            OpenMonthCalendarCommand = new Command<DateTime>(async (date) => await OpenMonthCalendarAsync(date));
            ViewEventDetailsCommand = new Command<CalendarEvent>(async (calendarEvent) => await ViewEventDetailsAsync(calendarEvent));
            PostponeEventCommand = new Command<CalendarEvent>(async (calendarEvent) => await PostponeEventAsync(calendarEvent));
            ShowEventDetailsCommand = new Command<CalendarEvent>(async (calendarEvent) => await ShowEventDetailsAsync(calendarEvent));
            LoadMoreDatesCommand = new Command(async () => await LoadMoreDatesAsync(), () => !IsLoading);
            GoToTodayCommand = new Command(ExecuteGoToToday);
            
            // Initial data loading
            LoadCalendarDays();
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
                    OnPropertyChanged(nameof(CurrentMonthName));
                    OnPropertyChanged(nameof(CurrentYear));
                }
            }
        }
        
        public string FormattedSelectedDate => SelectedDate.ToString("ddd, MMM d");
        
        public string FormattedTodayDate => DateTime.Today.ToString("ddd, MMM d");
        
        public string CurrentMonthName => SelectedDate.ToString("MMMM");
        
        public string CurrentYear => SelectedDate.ToString("yyyy");
        
        public ObservableCollection<DateTime> CalendarDays
        {
            get => _calendarDays;
            set => SetProperty(ref _calendarDays, value);
        }
        
        public ObservableCollection<DateTime> SelectableDates => CalendarDays;
        
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
        
        // Restriction-related properties
        private bool _hasActiveRestriction;
        public bool HasActiveRestriction
        {
            get => _hasActiveRestriction;
            set => SetProperty(ref _hasActiveRestriction, value);
        }
        
        private Color _restrictionBackgroundColor = Colors.LightSalmon;
        public Color RestrictionBackgroundColor
        {
            get => _restrictionBackgroundColor;
            set => SetProperty(ref _restrictionBackgroundColor, value);
        }
        
        // Using Material Icons glyphs instead of external PNG images to avoid missing-resource warnings
        // "\ue002" – warning, "\ue88e" – info
        private string _restrictionIcon = "\ue88e";
        public string RestrictionIcon
        {
            get => _restrictionIcon;
            set => SetProperty(ref _restrictionIcon, value);
        }
        
        private string _currentRestrictionText = "No active restrictions";
        public string CurrentRestrictionText
        {
            get => _currentRestrictionText;
            set => SetProperty(ref _currentRestrictionText, value);
        }
        
        // Selected date events
        private ObservableCollection<CalendarEvent> _eventsForSelectedDate;
        public ObservableCollection<CalendarEvent> EventsForSelectedDate
        {
            get => _eventsForSelectedDate ?? (_eventsForSelectedDate = new ObservableCollection<CalendarEvent>());
            set => SetProperty(ref _eventsForSelectedDate, value);
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
                
                // Начинаем с сегодняшней даты
                var startDate = DateTime.Today;
                _logger.LogInformation($"Start date: {startDate:yyyy-MM-dd}");

                // Генерируем даты на год вперёд
                var days = new List<DateTime>();
                for (int i = 0; i < InitialDaysToLoad; i++)
                {
                    days.Add(startDate.AddDays(i));
                }
                
                CalendarDays = new ObservableCollection<DateTime>(days);
                _lastLoadedDate = days.Last();
                
                _logger.LogInformation($"Loaded {days.Count} days from {days.First():yyyy-MM-dd} to {days.Last():yyyy-MM-dd}");

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
                _cacheService.CleanupOldEntries(30);
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
                if (TryGetCache(date, out var cachedEvents, out var lastUpd) &&
                    (DateTimeOffset.Now - lastUpd) <= _cacheExpiration)
                {
                    // Use cached data
                    _logger.LogInformation("Cache hit for date {Date}. Events count: {Count}", date.ToShortDateString(), cachedEvents.Count);
                    UpdateEventCountsForDate(date, cachedEvents);
                    continue;
                }
                _logger.LogInformation("Cache miss for date {Date}", date.ToShortDateString());

                // Load from service with retry
                var events = await _eventLoader.LoadEventsForDateAsync(date, cancellationToken);
                
                // Update cache and counts
                SetCache(date, events);
                _logger.LogInformation("Cached {Count} events for date {Date}", events.Count, date.ToShortDateString());
                UpdateEventCountsForDate(date, events);
            }
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
            _cacheService.CleanupOldEntries(30);
        }
        
        public async Task LoadTodayEventsAsync()
        {
            // Защита от слишком частых запросов (throttling)
            var now = DateTime.UtcNow;
            if ((now - _lastRefreshTime) < _throttleInterval)
            {
                _throttledRequests++;
                _logger.LogDebug("Request throttled. Last refresh was {ElapsedTime}ms ago. Total throttled: {ThrottledRequests}", 
                    (now - _lastRefreshTime).TotalMilliseconds, _throttledRequests);
                return;
            }
            
            // Проверка, активен ли уже другой запрос
            if (!await _refreshSemaphore.WaitAsync(0))
            {
                _concurrentRejections++;
                _logger.LogInformation("Refresh operation already in progress. Total rejections: {ConcurrentRejections}", _concurrentRejections);
                return;
            }
            
            try
            {
                _totalRequests++;
                _lastRefreshTime = now;
                _logger.LogDebug("Starting LoadTodayEventsAsync request #{TotalRequests}", _totalRequests);
                
                // Отмена предыдущих операций
                _refreshCancellationTokenSource?.Cancel();
                _refreshCancellationTokenSource?.Dispose();
                _refreshCancellationTokenSource = new CancellationTokenSource(RefreshTimeoutMilliseconds);
                var cancellationToken = _refreshCancellationTokenSource.Token;
                
                // Проверка кэша перед загрузкой
                DateTime selectedDateKey = SelectedDate.Date;
                if (TryGetCache(selectedDateKey, out var cachedEvents, out var lastUpdateTime))
                {
                    // Если кэш обновлялся недавно (менее 1 минуты назад), используем его
                    if ((DateTimeOffset.Now - lastUpdateTime) <= TimeSpan.FromMinutes(1))
                    {
                        _cacheHits++;
                        _logger.LogInformation("Using cached data for {Date}, cached {TimeAgo} seconds ago. Cache hits: {CacheHits}/{TotalRequests} ({HitPercentage}%)", 
                            selectedDateKey.ToShortDateString(), 
                            (DateTimeOffset.Now - lastUpdateTime).TotalSeconds,
                            _cacheHits, _totalRequests,
                            (int)((_cacheHits / (float)_totalRequests) * 100));
                        
                        await UpdateUIWithEvents(cachedEvents, cancellationToken);
                        return;
                    }
                    
                    // Если кэш существует, но устарел, обновляем UI сразу кэшированными данными,
                    // а затем запускаем фоновое обновление
                    await UpdateUIWithEvents(cachedEvents, cancellationToken);
                    _logger.LogInformation("Using stale cache while refreshing for {Date}", selectedDateKey.ToShortDateString());
                }
                else
                {
                    _cacheMisses++;
                    _logger.LogInformation("Cache miss for {Date}. Total misses: {CacheMisses}/{TotalRequests} ({MissPercentage}%)", 
                        selectedDateKey.ToShortDateString(),
                        _cacheMisses, _totalRequests,
                        (int)((_cacheMisses / (float)_totalRequests) * 100));
                }
                
                List<CalendarEvent> events = null;
                for (int retryCount = 0; retryCount <= MaxRetryAttempts; retryCount++)
                {
                    try
                    {
                        if (retryCount > 0)
                        {
                            _logger.LogInformation("Retrying load events for {Date} (Attempt {RetryCount}/{MaxRetries})", 
                                selectedDateKey.ToShortDateString(), retryCount, MaxRetryAttempts);
                        }
                        else 
                        {
                            _logger.LogInformation("Loading events for {Date}", selectedDateKey.ToShortDateString());
                        }
                        
                        // Загрузка событий для выбранной даты
                        events = (await _calendarService.GetEventsForDateAsync(selectedDateKey)).ToList();
                        
                        // Проверка отмены
                        cancellationToken.ThrowIfCancellationRequested();
                        break; // Выход из цикла при успешной загрузке
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("Refresh operation cancelled");
                        return; // Выходим без ошибки, операция просто отменена
                    }
                    catch (Exception ex) when (retryCount < MaxRetryAttempts)
                    {
                        _logger.LogWarning(ex, "Error loading events (Attempt {RetryCount}/{MaxRetries})", 
                            retryCount + 1, MaxRetryAttempts + 1);
                        
                        await Task.Delay(RetryDelays[retryCount], cancellationToken);
                        continue;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to load events after all retry attempts");
                        
                        // Если есть кэшированные данные, используем их даже устаревшие, чтобы не показывать пустой экран
                        if (TryGetCache(selectedDateKey, out cachedEvents, out _))
                        {
                            _logger.LogInformation("Using stale cache after error for {Date}", selectedDateKey.ToShortDateString());
                            await UpdateUIWithEvents(cachedEvents, cancellationToken);
                        }
                        else
                        {
                            _logger.LogInformation("No cached data available for {Date} after error", selectedDateKey.ToShortDateString());
                            // Обновляем UI с пустым списком событий
                            await UpdateUIWithEvents(new List<CalendarEvent>(), cancellationToken);
                        }
                        
                        // Показываем сообщение об ошибке только если нет кэша
                        if (cachedEvents == null || !cachedEvents.Any())
                        {
                            await Application.Current.MainPage.Dispatcher.DispatchAsync(async () =>
                            {
                                await Application.Current.MainPage.DisplayAlert(
                                    "Error",
                                    "Failed to refresh events. Using cached data if available.",
                                    "OK"
                                );
                            });
                        }
                        
                        return;
                    }
                }
                
                // Если успешно загрузили данные, обновляем кэш и UI
                if (events != null)
                {
                    // Обновление кэша атомарно
                    SetCache(selectedDateKey, events);
                    
                    await UpdateUIWithEvents(events, cancellationToken);
                    _logger.LogInformation("Successfully loaded and cached {Count} events for {Date}", 
                        events.Count(), selectedDateKey.ToShortDateString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in LoadTodayEventsAsync");
            }
            finally
            {
                IsRefreshing = false;
                
                // Очищаем старые записи из кэша (старше 24 часов)
                CleanupCacheEntries();
                
                // Освобождаем семафор
                _refreshSemaphore.Release();
            }
        }
        
        private async Task UpdateUIWithEvents(IEnumerable<CalendarEvent> events, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            await Application.Current.MainPage.Dispatcher.DispatchAsync(() =>
            {
                var eventsList = events?.ToList() ?? new List<CalendarEvent>();
                FlattenedEvents = new ObservableCollection<CalendarEvent>(eventsList);
                SortedEvents = new ObservableCollection<CalendarEvent>(
                    eventsList.OrderBy(e => e.Date.TimeOfDay).ToList());
                EventsForSelectedDate = new ObservableCollection<CalendarEvent>(eventsList);

                var (prog, percent) = _progressCalculator.CalculateProgress(eventsList);
                CompletionProgress = prog;
                CompletionPercentage = percent;
                OnPropertyChanged(nameof(FlattenedEvents));
                OnPropertyChanged(nameof(SortedEvents));
                OnPropertyChanged(nameof(EventsForSelectedDate));

                // Логируем актуальное количество после обновления
                _logger.LogDebug("UpdateUIWithEvents: Updated FlattenedEvents with {Count} events for date {Date}", FlattenedEvents.Count, SelectedDate.ToShortDateString());
            });
        }
        
        private void CleanupCacheEntries()
        {
            // Delegate cache cleanup to service
            _cacheService.CleanupOldEntries(30);
        }
        
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
            {
                _logger.LogWarning("LoadEventCountsForVisibleDaysAsync called with empty CalendarDays");
                return;
            }

            try
            {
                _eventCountsRequests++;
                _logger.LogInformation("LoadEventCountsForVisibleDaysAsync started (request #{Count}). Range: {StartDate} to {EndDate}", 
                    _eventCountsRequests, 
                    CalendarDays.First().ToShortDateString(), 
                    CalendarDays.Last().ToShortDateString());
                
                // Создаем CancellationTokenSource с таймаутом
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var cancellationToken = cts.Token;
                
                // Подготовка результирующего словаря для всего диапазона дат
                var result = new Dictionary<DateTime, Dictionary<EventType, int>>();
                
                // Инициализируем словарь для всех дней с нулевыми счетчиками
                foreach (var day in CalendarDays)
                {
                    result[day.Date] = new Dictionary<EventType, int>
                    {
                        { EventType.MedicationTreatment, 0 },
                        { EventType.Photo, 0 },
                        { EventType.CriticalWarning, 0 },
                        { EventType.Video, 0 },
                        { EventType.MedicalVisit, 0 },
                        { EventType.GeneralRecommendation, 0 }
                    };
                }
                
                // 1. Разделяем даты на "кэшированные" и "требующие загрузки"
                var cachedDates = new List<DateTime>();
                var datesToLoad = new List<DateTime>();
                
                foreach (var day in CalendarDays)
                {
                    var date = day.Date;
                    if (TryGetCache(date, out var cachedEvents, out var lastUpdate) &&
                        (DateTimeOffset.Now - lastUpdate) <= TimeSpan.FromMinutes(10))
                    {
                        // Дата уже есть в кэше и обновлялась недавно
                        cachedDates.Add(date);
                    }
                    else
                    {
                        // Дата отсутствует в кэше или устарела
                        datesToLoad.Add(date);
                    }
                }
                
                _logger.LogInformation("Dates analysis: {CachedCount} cached, {ToLoadCount} to load", 
                    cachedDates.Count, datesToLoad.Count);
                
                // 2. Обрабатываем кэшированные даты (если они есть)
                if (cachedDates.Any())
                {
                    _eventCountsCacheHits++;
                    _logger.LogInformation("Using cached data for {Count} dates. Cache hit rate: {Rate}%", 
                        cachedDates.Count, 
                        (int)((_eventCountsCacheHits / (float)_eventCountsRequests) * 100));
                    
                    foreach (var date in cachedDates)
                    {
                        if (TryGetCache(date, out var events, out _))
                        {
                            UpdateEventCounts(result, events);
                        }
                    }
                }
                
                // 3. Группируем даты, требующие загрузки, в смежные диапазоны
                if (datesToLoad.Any())
                {
                    var dateRanges = GroupDatesIntoRanges(datesToLoad);
                    _logger.LogInformation("Grouped {DatesToLoad} dates into {RangeCount} request ranges", 
                        datesToLoad.Count, dateRanges.Count);
                    
                    _eventCountsBatchRequests += dateRanges.Count;
                    
                    // 4. Загружаем данные для каждого диапазона дат
                    foreach (var range in dateRanges)
                    {
                        try
                        {
                            _logger.LogInformation("Loading events for date range: {StartDate} to {EndDate}", 
                                range.startDate.ToShortDateString(), range.endDate.ToShortDateString());
                            
                            // Загружаем данные для диапазона дат
                            var rangeEvents = await _calendarService.GetEventsForDateRangeAsync(range.startDate, range.endDate);
                            
                            if (cancellationToken.IsCancellationRequested)
                            {
                                _logger.LogWarning("Loading events was cancelled");
                                break;
                            }
                            
                            // Обновляем результаты для этого диапазона
                            UpdateEventCounts(result, rangeEvents);
                            
                            // Обновляем кэш для каждой даты в диапазоне
                            UpdateCacheForDateRange(range.startDate, range.endDate, rangeEvents);
                            
                            _logger.LogInformation("Successfully loaded {Count} events for range {StartDate} to {EndDate}",
                                rangeEvents.Count(), range.startDate.ToShortDateString(), range.endDate.ToShortDateString());
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error loading events for date range {StartDate} to {EndDate}", 
                                range.startDate.ToShortDateString(), range.endDate.ToShortDateString());
                            
                            // Продолжаем с другими диапазонами, но не прерываем выполнение метода
                        }
                    }
                }
                
                // 5. Обновляем общий результат
                await Application.Current.MainPage.Dispatcher.DispatchAsync(() =>
                {
                    EventCountsByDate = result;
                    OnPropertyChanged(nameof(EventCountsByDate));
                });
                
                _logger.LogInformation("LoadEventCountsForVisibleDaysAsync completed. Processed {DateCount} dates", 
                    result.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LoadEventCountsForVisibleDaysAsync");
                
                // Возвращаем частичный результат, если он есть
                if (EventCountsByDate == null || !EventCountsByDate.Any())
                {
                    // Если у нас совсем нет данных, создаем пустой результат
                    var emptyResult = new Dictionary<DateTime, Dictionary<EventType, int>>();
                    
                    foreach (var day in CalendarDays)
                    {
                        emptyResult[day.Date] = new Dictionary<EventType, int>
                        {
                            { EventType.MedicationTreatment, 0 },
                            { EventType.Photo, 0 },
                            { EventType.CriticalWarning, 0 },
                            { EventType.Video, 0 },
                            { EventType.MedicalVisit, 0 },
                            { EventType.GeneralRecommendation, 0 }
                        };
                    }
                    
                    EventCountsByDate = emptyResult;
                    OnPropertyChanged(nameof(EventCountsByDate));
                }
            }
        }
        
        private List<(DateTime startDate, DateTime endDate)> GroupDatesIntoRanges(List<DateTime> dates)
        {
            if (dates == null || !dates.Any())
                return new List<(DateTime, DateTime)>();
                
            var sortedDates = dates.OrderBy(d => d).ToList();
            var result = new List<(DateTime startDate, DateTime endDate)>();
            
            DateTime rangeStart = sortedDates[0];
            DateTime rangeEnd = rangeStart;
            
            for (int i = 1; i < sortedDates.Count; i++)
            {
                var currentDate = sortedDates[i];
                
                // Если текущая дата следует сразу за предыдущей, расширяем диапазон
                if ((currentDate - rangeEnd).TotalDays <= 1)
                {
                    rangeEnd = currentDate;
                }
                else
                {
                    // Иначе закрываем текущий диапазон и начинаем новый
                    result.Add((rangeStart, rangeEnd));
                    rangeStart = currentDate;
                    rangeEnd = currentDate;
                }
            }
            
            // Добавляем последний диапазон
            result.Add((rangeStart, rangeEnd));
            
            return result;
        }
        
        private void UpdateCacheForDateRange(DateTime startDate, DateTime endDate, IEnumerable<CalendarEvent> events)
        {
            var eventsList = events?.ToList() ?? new List<CalendarEvent>();
            if (!eventsList.Any()) return; // Если событий нет, ничего не делаем с кэшем

            // Группируем ПОЛУЧЕННЫЕ события по датам
            var eventsByDate = eventsList.GroupBy(e => e.Date.Date)
                                     .ToDictionary(g => g.Key, g => g.ToList());

            // Обновляем кэш ТОЛЬКО для тех дат, для которых пришли события
            foreach (var kvp in eventsByDate)
            {
                var date = kvp.Key;
                var dateEvents = kvp.Value;
                if (date >= startDate.Date && date <= endDate.Date)
                {
                    SetCache(date, dateEvents);
                }
            }
            // Мы НЕ итерируем по всем датам диапазона и НЕ перезаписываем кэш пустыми списками.
        }
        
        private void UpdateEventCounts(Dictionary<DateTime, Dictionary<EventType, int>> result, IEnumerable<CalendarEvent> events)
        {
            foreach (var evt in events)
            {
                if (evt.IsMultiDay && evt.EndDate.HasValue)
                {
                    // Обрабатываем многодневные события
                    var currentDate = evt.Date.Date;
                    var endDate = evt.EndDate.Value.Date;
                    
                    while (currentDate <= endDate)
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
                    // Обрабатываем однодневные события
                    var eventDate = evt.Date.Date;
                    if (result.ContainsKey(eventDate))
                    {
                        result[eventDate][evt.EventType]++;
                    }
                }
            }
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
                try
                {
                    // Capture original state for rollback
                    var originalState = calendarEvent.IsCompleted;
                    
                    try
                    {
                        // Toggle state first
                        calendarEvent.IsCompleted = !calendarEvent.IsCompleted;
                        
                        // Call service with single parameter
                        await _calendarService.MarkEventAsCompletedAsync(calendarEvent.Id);
                        
                        // Update cache if needed
                        if (TryGetCache(SelectedDate.Date, out var cachedEvents, out _))
                        {
                            // Update cache list in service
                            var eventToUpdate = cachedEvents.FirstOrDefault(e => e.Id == calendarEvent.Id);
                            if (eventToUpdate != null)
                            {
                                eventToUpdate.IsCompleted = calendarEvent.IsCompleted;
                                SetCache(SelectedDate.Date, cachedEvents);
                            }
                        }
                        
                        // Update overdue events counter if needed
                        if (calendarEvent.Date.Date < DateTime.Today)
                        {
                            await CheckOverdueEventsAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Rollback on error
                        _logger.LogError(ex, "Error toggling event completion");
                        calendarEvent.IsCompleted = originalState;
                        
                        await Application.Current.MainPage.Dispatcher.DispatchAsync(async () =>
                        {
                            await Application.Current.MainPage.DisplayAlert(
                                "Error",
                                "Failed to update event status. Please try again.",
                                "OK"
                            );
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in ToggleEventCompletionAsync");
                }
            }
        }
        
        private async Task SelectDateAsync(DateTime date)
        {
            if (date.Date == SelectedDate.Date)
            {
                _logger.LogDebug("SelectDateAsync: Same date selected, ignoring");
                return;
            }

            Debug.WriteLine($"SelectDateAsync called with date: {date.ToShortDateString()}");
            Debug.WriteLine($"Current SelectedDate before change: {SelectedDate.ToShortDateString()}");

            // Сохраняем старую дату для возможного отката
            var previousDate = SelectedDate;

            try
            {
                // Обновляем дату (это вызовет изменение UI)
                SelectedDate = date;
                OnPropertyChanged(nameof(SelectedDate));
                OnPropertyChanged(nameof(FormattedSelectedDate));

                // Уведомляем о выбранной дате и вызываем ScrollToIndexTarget для центрирования
                ScrollToIndexTarget = date;
                
                // Сохраняем выбранную дату в настройках
                SaveSelectedDate(date);

                Debug.WriteLine($"SelectedDate after change: {SelectedDate.ToShortDateString()}");

                // Загружаем события для выбранной даты
                await LoadTodayEventsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error selecting date {Date}", date.ToShortDateString());

                // Возвращаем предыдущую дату в случае ошибки
                if (previousDate != date)
                {
                    SelectedDate = previousDate;
                    OnPropertyChanged(nameof(SelectedDate));
                    OnPropertyChanged(nameof(FormattedSelectedDate));
                }
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

        private async Task RefreshDataAsync()
        {
            if (IsRefreshing)
                return;

            try
            {
                IsRefreshing = true;
                await LoadTodayEventsAsync();
                await LoadEventCountsForVisibleDaysAsync();
                await CheckOverdueEventsAsync();
                await CheckAndLoadActiveRestrictionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing data");
                CurrentLoadingState = LoadingState.Error;
                LoadingStatus = "Error refreshing data. Please try again.";
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private void ExecuteGoToToday()
        {
            try
            {
                _logger.LogInformation("GoToTodayCommand executed.");
                // Set SelectedDate to trigger event loading and UI updates
                SelectedDate = DateTime.Today;

                // Set the target property to trigger scroll in the View
                ScrollToIndexTarget = DateTime.Today;
                // Log the value safely BEFORE it might be reset by the handler
                _logger.LogInformation($"ScrollToIndexTarget set to {DateTime.Today.ToShortDateString()}");
                
                // Make sure the date gets visually selected in the DateSelector
                // by explicitly raising property changed for SelectedDate
                OnPropertyChanged(nameof(SelectedDate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing GoToTodayCommand.");
            }
        }

        // Add a new public method that can be called to initialize data
        public async Task InitializeDataAsync()
        {
            if (_isRefreshingData)
                return;

            _isRefreshingData = true;
            IsLoading = true;

            try
            {
                await LoadTodayEventsAsync();
                await LoadEventCountsForVisibleDaysAsync();
                await CheckOverdueEventsAsync();
                await CheckAndLoadActiveRestrictionsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading initial data: {ex.Message}");
            }
            finally
            {
                _isRefreshingData = false;
                IsLoading = false;
            }
        }

        private async Task CheckAndLoadActiveRestrictionsAsync()
        {
            try
            {
                // Get any active restrictions
                var activeRestrictions = await _calendarService.GetActiveRestrictionsAsync();
                
                // Update UI properties based on restrictions
                HasActiveRestriction = activeRestrictions != null && activeRestrictions.Any();
                
                if (HasActiveRestriction && activeRestrictions.Count > 0)
                {
                    // Use the most critical restriction if there are multiple
                    var criticalRestriction = activeRestrictions.FirstOrDefault(r => r.EventType == EventType.CriticalWarning) 
                                             ?? activeRestrictions.First();
                    
                    CurrentRestrictionText = criticalRestriction.Description;
                    
                    // Set appropriate colors based on restriction type
                    switch (criticalRestriction.EventType)
                    {
                        case EventType.CriticalWarning:
                            RestrictionBackgroundColor = Color.FromArgb("#FFEBEE"); // Light red
                            RestrictionIcon = "\ue002"; // warning glyph
                            break;
                        default:
                            RestrictionBackgroundColor = Color.FromArgb("#FFF8E1"); // Light amber
                            RestrictionIcon = "\ue88e"; // info glyph
                            break;
                    }
                }
                else
                {
                    // Default values when no restrictions
                    CurrentRestrictionText = "No active restrictions";
                    RestrictionBackgroundColor = Colors.LightSalmon;
                    RestrictionIcon = "\ue88e"; // info glyph
                }
                
                _logger.LogInformation("Active restrictions check completed. HasActiveRestriction: {HasActiveRestriction}", HasActiveRestriction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking active restrictions");
                HasActiveRestriction = false;
                CurrentRestrictionText = "Unable to check restrictions";
            }
        }

        #region CacheHelpers
        private bool TryGetCache(DateTime date, out List<CalendarEvent> events, out DateTimeOffset lastUpdate) =>
            _cacheService.TryGet(date, out events, out lastUpdate);

        private void SetCache(DateTime date, IEnumerable<CalendarEvent> events) => _cacheService.Set(date, events);
        #endregion
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