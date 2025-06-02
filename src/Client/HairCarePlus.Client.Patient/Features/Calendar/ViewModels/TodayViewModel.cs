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
using HairCarePlus.Shared.Common.CQRS;
using HairCarePlus.Client.Patient.Features.Calendar.Application.Commands;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel; // for MainThread
using HairCarePlus.Client.Patient.Features.Calendar.Application.Queries;
using ICommand = System.Windows.Input.ICommand;
using CalendarCommands = HairCarePlus.Client.Patient.Features.Calendar.Application.Commands;
using HairCarePlus.Client.Patient.Infrastructure.Services.Interfaces;
using HairCarePlus.Client.Patient.Common.Utils;
using HairCarePlus.Client.Patient.Features.Calendar.Helpers;
using HairCarePlus.Client.Patient.Common.Services;

// Alias to disambiguate with namespace HairCarePlus.Client.Patient.Features.Calendar.Application
using MauiApp = Microsoft.Maui.Controls.Application;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    public class TodayViewModel : BaseViewModel
    {
        private readonly ICalendarService _calendarService;
        private readonly ICalendarCacheService _cacheService;
        private readonly ICalendarLoader _eventLoader;
        private readonly IProgressCalculator _progressCalculator;
        private readonly ILogger<TodayViewModel> _logger;
        private readonly IMessenger _messenger;
        private readonly ICommandBus _commandBus;
        private readonly IQueryBus _queryBus;
        private readonly IProfileService _profileService;
        private readonly IPreloadingService _preloadingService;
        private readonly IConfettiManager _confettiManager;
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
        private const int PreDaysToLoad = 30; // количество дней ДО операции/сегодня
        private const int InitialDaysToLoad = 90; // дни ВПЕРЁД (3 месяца вместо года)
        private const int DaysToLoadMore = 60; // подгрузка вперёд
        private const int MaxTotalDays = PreDaysToLoad + InitialDaysToLoad + 365; // расширенный лимит (≈2 года)
        private DateTime _lastLoadedDate;

        // New fields for enhanced functionality
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(1);
        private CancellationTokenSource? _loadingCancellationSource;
        private int _loadingProgress;
        private string _loadingStatus;
        private const int MaxRetryAttempts = 3;
        private const int BatchSize = 10;
        private const int RefreshTimeoutMilliseconds = 30000; // 30 seconds timeout
        
        // Поля для оптимизации обновлений
        private bool _isUpdatingDateProperties;
        private DateDisplayInfo? _cachedDateDisplayInfo;
        
        // Группированное свойство для отображения информации о дате
        public DateDisplayInfo DateDisplayProperties
        {
            get
            {
                if (_cachedDateDisplayInfo == null || _isUpdatingDateProperties)
                {
                    _cachedDateDisplayInfo = new DateDisplayInfo
                    {
                        FormattedSelectedDate = SelectedDate.ToString("ddd, MMM d"),
                        CurrentMonthName = VisibleDate.ToString("MMMM"),
                        CurrentYear = VisibleDate.ToString("yyyy"),
                        DaysSinceTransplant = (SelectedDate.Date - _profileService.SurgeryDate.Date).Days + 1,
                        DaysSinceTransplantSubtitle = $"Day {((SelectedDate.Date - _profileService.SurgeryDate.Date).Days + 1)} post hair transplant"
                    };
                }
                return _cachedDateDisplayInfo;
            }
        }

        private DateTime _visibleDate;
        /// <summary>
        /// Date used for displaying current month/year in header. It updates when user scrolls horizontally or selects a date.
        /// </summary>
        public DateTime VisibleDate
        {
            get => _visibleDate;
            set
            {
                if (SetProperty(ref _visibleDate, value))
                {
                    OnPropertyChanged(nameof(CurrentMonthName));
                    OnPropertyChanged(nameof(CurrentYear));
                }
            }
        }

        public TodayViewModel(ICalendarService calendarService, ICalendarCacheService cacheService, ICalendarLoader eventLoader, IProgressCalculator progressCalculator, ILogger<TodayViewModel> logger, ICommandBus commandBus, IQueryBus queryBus, IMessenger messenger, IProfileService profileService, IPreloadingService preloadingService, IConfettiManager confettiManager)
        {
            _calendarService = calendarService;
            _cacheService = cacheService;
            _eventLoader = eventLoader;
            _progressCalculator = progressCalculator;
            _logger = logger;
            _messenger = messenger;
            _commandBus = commandBus;
            _queryBus = queryBus;
            _profileService = profileService;
            _preloadingService = preloadingService;
            _confettiManager = confettiManager;
            
            // Инициализируем коллекции
            _calendarDays = new ObservableCollection<DateTime>();
            _todayEvents = new ObservableCollection<GroupedCalendarEvents>();
            _flattenedEvents = new ObservableCollection<CalendarEvent>();
            _sortedEvents = new ObservableCollection<CalendarEvent>();
            _eventsForSelectedDate = new ObservableCollection<CalendarEvent>();
            
            // Всегда начинаем с сегодняшней даты (игнорируем сохранённое состояние прошлой сессии)
            _selectedDate = DateTime.Today;
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
            
            VisibleDate = _selectedDate; // initialize visible date for header
            
            // Initialize commands
            RefreshCommand = new Command(async () => await RefreshDataAsync());
            ToggleEventCompletionCommand = new AsyncRelayCommand<CalendarEvent>(async (calendarEvent) =>
            {
                _logger?.LogInformation("🔥 ToggleEventCompletionCommand EXECUTED in constructor lambda. Event: {EventTitle}", calendarEvent?.Title ?? "null");
                if (calendarEvent == null) return;
                await ToggleEventCompletionAsync(calendarEvent);
            });
            SelectDateCommand = new Command<DateTime>(async (date) => await SelectDateAsync(date));
            OpenMonthCalendarCommand = new Command<DateTime>(async (date) => await OpenMonthCalendarAsync(date));
            ViewEventDetailsCommand = new Command<CalendarEvent>(async (calendarEvent) => 
            {
                _logger?.LogInformation("👆 ViewEventDetailsCommand EXECUTED in constructor lambda");
                await ViewEventDetailsAsync(calendarEvent);
            });
            PostponeEventCommand = new Command<CalendarEvent>(async (calendarEvent) => await PostponeEventAsync(calendarEvent));
            ShowEventDetailsCommand = new Command<CalendarEvent>(async (calendarEvent) => await ShowEventDetailsAsync(calendarEvent));
            LoadMoreDatesCommand = new Command(async () => await LoadMoreDatesAsync(), () => !IsLoading);
            GoToTodayCommand = new Command(ExecuteGoToToday);
            
            // Toggle restrictions visibility
            ToggleRestrictionsVisibilityCommand = new Command(() =>
            {
                AreRestrictionsVisible = !AreRestrictionsVisible;
            });
            
            // Lazy initialization – фактическая загрузка отложена до первого OnAppearing
            _initializationTask = null;

            // Subscribe to event update messages to refresh UI
            _messenger.Register<EventUpdatedMessage>(this, async (recipient, message) =>
            {
                try
                {
                    _logger?.LogDebug("Received EventUpdatedMessage for EventId={EventId}", message.Value);

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        // Находим событие в коллекциях
                        var eventToUpdate = FlattenedEvents.FirstOrDefault(e => e.Id == message.Value);
                        var eventInMaster = _allEventsForSelectedDate?.FirstOrDefault(e => e.Id == message.Value);
                        
                        // Загружаем событие для получения актуального состояния
                        CalendarEvent? updatedEvent = null;
                        try
                        {
                            updatedEvent = await _queryBus.SendAsync<CalendarEvent?>(
                                new GetEventByIdQuery(message.Value));
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Error loading event {EventId}", message.Value);
                        }
                        
                        if (updatedEvent != null)
                        {
                            // Обновляем в мастер-списке
                            if (eventInMaster != null)
                            {
                                eventInMaster.IsCompleted = updatedEvent.IsCompleted;
                            }
                            
                            // Обновляем в видимых коллекциях
                            if (eventToUpdate != null)
                            {
                                if (updatedEvent.IsCompleted)
                                {
                                    // Удаляем завершенное событие
                                    FlattenedEvents.Remove(eventToUpdate);
                                    SortedEvents.Remove(eventToUpdate);
                                    EventsForSelectedDate.Remove(eventToUpdate);
                                    _logger?.LogDebug("Removed completed event {EventId} from visible collections", message.Value);
                                }
                                else
                                {
                                    // Обновляем свойства
                                    eventToUpdate.IsCompleted = updatedEvent.IsCompleted;
                                    eventToUpdate.Title = updatedEvent.Title;
                                    eventToUpdate.Description = updatedEvent.Description;
                                    _logger?.LogDebug("Updated event {EventId} properties", message.Value);
                                }
                            }
                            else if (!updatedEvent.IsCompleted && 
                                     updatedEvent.Date.Date == SelectedDate.Date && 
                                     updatedEvent.EventType != EventType.CriticalWarning)
                            {
                                // Новое невыполненное событие для текущей даты
                                FlattenedEvents.Add(updatedEvent);
                                
                                // Вставляем в правильное место в отсортированной коллекции
                                var insertIndex = SortedEvents.TakeWhile(e => e.Date.TimeOfDay < updatedEvent.Date.TimeOfDay).Count();
                                SortedEvents.Insert(insertIndex, updatedEvent);
                                
                                EventsForSelectedDate.Add(updatedEvent);
                                
                                // Добавляем в мастер-список
                                _allEventsForSelectedDate?.Add(updatedEvent);
                                
                                _logger?.LogDebug("Added new event {EventId} to collections", message.Value);
                            }
                        }
                        
                        // Пересчитываем прогресс только если есть изменения
                        if (_allEventsForSelectedDate != null)
                        {
                            var (prog, percent) = _progressCalculator.CalculateProgress(_allEventsForSelectedDate);
                            if (Math.Abs(CompletionProgress - prog) > 0.01)
                            {
                                CompletionProgress = prog;
                                CompletionPercentage = percent;
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error handling EventUpdatedMessage");
                }
            });
        }
        
        /// <summary>
        /// Группирует обновления свойств, связанных с датой, для уменьшения количества перерисовок UI
        /// </summary>
        private void BatchUpdateDateProperties(Action updateAction)
        {
            _isUpdatingDateProperties = true;
            _cachedDateDisplayInfo = null; // Сбрасываем кэш
            try
            {
                updateAction();
            }
            finally
            {
                _isUpdatingDateProperties = false;
                // Одно обновление вместо множественных
                OnPropertyChanged(nameof(DateDisplayProperties));
            }
        }
        
        private Task? _initializationTask;
        private readonly object _initLock = new();

        public Task EnsureLoadedAsync()
        {
            lock (_initLock)
            {
                _initializationTask ??= LoadInitialAsync();
                return _initializationTask;
            }
        }

        private async Task LoadInitialAsync()
        {
            try
            {
                // 1. Загружаем календарные дни на год вперёд
                LoadCalendarDays();

                // 2. Устанавливаем текущий день как выбранный
                var today = DateTime.Today;
                SelectedDate = today;
                VisibleDate = today;

                // 3. Подгружаем события для выбранной даты
                await LoadTodayEventsAsync();

                // 4. Загружаем активные ограничения
                await CheckAndLoadActiveRestrictionsAsync();
                
                // 5. Запускаем фоновую предзагрузку
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _preloadingService.StartBackgroundPreloadingAsync();
                        // Предзагружаем ближайшие даты
                        await _preloadingService.PreloadDateRangeAsync(
                            DateTime.Today.AddDays(-7), 
                            DateTime.Today.AddDays(30));
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error starting background preloading");
                    }
                });

                // 6. Прокручиваем к выбранной дате
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OnPropertyChanged(nameof(SelectedDate));
                    OnPropertyChanged(nameof(VisibleDate));
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in LoadInitialAsync");
            }
        }
        
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                {
                    _logger?.LogInformation("SelectedDate setter: Date changed to {Value}", value);
#if DEBUG
                    Debug.WriteLine($"SelectedDate setter: {value.ToShortDateString()}");
#endif
                    
                    BatchUpdateDateProperties(() =>
                    {
                        if (value.Month != VisibleDate.Month || value.Year != VisibleDate.Year)
                        {
                            VisibleDate = value;
                        }
                    });
                    
                    // Немедленно сохраняем дату и стартуем предзагрузку соседних дней.
                    SaveSelectedDate(value);

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _preloadingService.PreloadAdjacentDatesAsync(value, daysBefore: 3, daysAfter: 3);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Failed to preload events for adjacent dates");
                        }
                    });
                }
            }
        }
        
        public string FormattedSelectedDate => SelectedDate.ToString("ddd, MMM d");
        
        public string FormattedTodayDate => DateTime.Today.ToString("ddd, MMM d");
        
        // NEW: Separate fields for today day number and day-of-week (used in header circle)
        public int TodayDay => DateTime.Today.Day;
        public string TodayDayOfWeek => DateTime.Today.ToString("ddd");
        
        public string CurrentMonthName => VisibleDate.ToString("MMMM");
        
        public string CurrentYear => VisibleDate.ToString("yyyy");
        
        // Added: SurgeryDate and DaysSinceTransplant for header subtitle
        public int DaysSinceTransplant => (SelectedDate.Date - _profileService.SurgeryDate.Date).Days + 1;
        public string DaysSinceTransplantSubtitle => $"Day {DaysSinceTransplant} post hair transplant";
        
        public ObservableCollection<DateTime> CalendarDays
        {
            get => _calendarDays;
            set
            {
                if (SetProperty(ref _calendarDays, value))
                {
                    // Notify that SelectableDates (alias) has changed as well
                    OnPropertyChanged(nameof(SelectableDates));
                }
            }
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
        
        // NEW: Collection for active restrictions for UI
        public ObservableCollection<RestrictionInfo> ActiveRestrictions { get; } = new();
        private bool _hasActiveRestriction; // Keep the backing field for the property
        
        // RESTORED Property definition (now triggers AreRestrictionsVisible update)
        public bool HasActiveRestriction
        {
            get => _hasActiveRestriction;
            private set
            {
                if (SetProperty(ref _hasActiveRestriction, value))
                {
                    OnPropertyChanged(nameof(AreRestrictionsVisible));
                }
            }
        }

        // Property to allow user to hide/show restrictions panel
        private bool _restrictionsVisible = true;
        public bool AreRestrictionsVisible
        {
            get => _restrictionsVisible && HasActiveRestriction;
            set => SetProperty(ref _restrictionsVisible, value);
        }

        public ICommand ToggleRestrictionsVisibilityCommand { get; }
        
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
        
        // Summary presentation of the FIRST (highest-priority) active restriction for legacy UI bindings
        private string _restrictionIcon;
        public string RestrictionIcon
        {
            get => _restrictionIcon;
            private set => SetProperty(ref _restrictionIcon, value);
        }

        private string _currentRestrictionText;
        public string CurrentRestrictionText
        {
            get => _currentRestrictionText;
            private set => SetProperty(ref _currentRestrictionText, value);
        }

        private Color _restrictionBackgroundColor = Colors.Transparent;
        public Color RestrictionBackgroundColor
        {
            get => _restrictionBackgroundColor;
            private set => SetProperty(ref _restrictionBackgroundColor, value);
        }

        private List<CalendarEvent> _allEventsForSelectedDate = new();

        private void LoadCalendarDays()
        {
            try
            {
                _logger.LogInformation("Starting LoadCalendarDays");
                
                // Начальная точка на PreDaysToLoad дней раньше сегодняшнего дня
                var startDate = DateTime.Today.AddDays(-PreDaysToLoad);
                _logger.LogInformation($"Start date: {startDate:yyyy-MM-dd}");

                // Генерируем диапазон: PreDaysToLoad назад + InitialDaysToLoad вперёд
                var days = new List<DateTime>();
                var totalToGenerate = PreDaysToLoad + InitialDaysToLoad;
                for (int i = 0; i < totalToGenerate; i++)
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
                    await MauiApp.Current.MainPage.Dispatcher.DispatchAsync(() =>
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
                    // Use cached data (debug-level to avoid log spam in production)
                    _logger.LogDebug("Cache hit for date {Date}. Events count: {Count}", date.ToShortDateString(), cachedEvents.Count);
                    UpdateEventCountsForDate(date, cachedEvents);
                    continue;
                }
                _logger.LogDebug("Cache miss for date {Date}", date.ToShortDateString());

                // Load from service with retry
                var events = await _eventLoader.LoadEventsForDateAsync(date, cancellationToken);
                
                // Update cache and counts
                SetCache(date, events);
                _logger.LogDebug("Cached {Count} events for date {Date}", events.Count, date.ToShortDateString());
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
        
        public async Task LoadTodayEventsAsync() => await LoadTodayEventsAsync(false);
        
        public async Task LoadTodayEventsAsync(bool skipThrottling)
        {
            var localSelectedDate = SelectedDate; // Capture current SelectedDate for this execution
            _logger?.LogInformation("LoadTodayEventsAsync: START for date {LocalSelectedDate}, skipThrottling={SkipThrottling}", localSelectedDate, skipThrottling);
            
            // Если skipThrottling = true, пропускаем проверку семафора
            if (!skipThrottling)
            {
                // Проверка, активен ли уже другой запрос
                if (!await _refreshSemaphore.WaitAsync(0))
                {
                    _concurrentRejections++;
                    _logger.LogInformation("Refresh operation already in progress. Total rejections: {ConcurrentRejections}", _concurrentRejections);
                    return;
                }
            }
            
            try
            {
                _totalRequests++;
                _lastRefreshTime = DateTime.UtcNow;
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
                    _logger.LogDebug("Cache miss for {Date}. Total misses: {CacheMisses}/{TotalRequests} ({MissPercentage}%)", 
                        selectedDateKey.ToShortDateString(),
                        _cacheMisses, _totalRequests,
                        (int)((_cacheMisses / (float)_totalRequests) * 100));
                }
                
                _logger?.LogInformation("LoadTodayEventsAsync: Attempting to fetch events for {SelectedDateKey}", selectedDateKey);
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
                            _logger.LogDebug("Loading events for {Date}", selectedDateKey.ToShortDateString());
                        }
                        
                        // Загрузка событий через CQRS QueryBus
                        events = (await _queryBus.SendAsync<IEnumerable<CalendarEvent>>(new GetEventsForDateQuery(selectedDateKey))).ToList();
                        
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
                            await MauiApp.Current.MainPage.Dispatcher.DispatchAsync(async () =>
                            {
                                await MauiApp.Current.MainPage.DisplayAlert(
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
                    _logger.LogDebug("Successfully loaded and cached {Count} events for {Date}", 
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
                
                // Освобождаем семафор только если мы его захватывали
                if (!skipThrottling)
                {
                    _refreshSemaphore.Release();
                }
            }
        }
        
        private async Task UpdateUIWithEvents(IEnumerable<CalendarEvent> events, CancellationToken cancellationToken)
        {
            _logger?.LogInformation("UpdateUIWithEvents: START. Received {Count} events. Current SelectedDate in VM: {SelectedDate}", events?.Count() ?? 0, _selectedDate.ToShortDateString());
            if (cancellationToken.IsCancellationRequested) return;

            // Подготавливаем данные вне UI потока для оптимизации
            var eventsList = events?.ToList() ?? new List<CalendarEvent>();
            var visibleEvents = eventsList.Where(e => !e.IsCompleted && e.EventType != EventType.CriticalWarning).ToList();
            var sortedVisibleEvents = visibleEvents.OrderBy(e => e.Date.TimeOfDay).ToList();
            
            // Вычисляем прогресс вне UI потока
            var (prog, percent) = _progressCalculator.CalculateProgress(eventsList);
            var shouldUpdateProgress = Math.Abs(CompletionProgress - prog) > 0.01;
            var shouldShowConfetti = percent == 100 && !_confettiManager.IsAnimating;

            await MauiApp.Current.MainPage.Dispatcher.DispatchAsync(() =>
            {
                // Быстрое обновление коллекций с уже подготовленными данными
                CollectionUpdater.UpdateCollection(
                    FlattenedEvents, 
                    visibleEvents, 
                    (a, b) => a.Id == b.Id);
                
                // Используем уже отсортированный список
                CollectionUpdater.UpdateCollection(
                    SortedEvents,
                    sortedVisibleEvents,
                    (a, b) => a.Id == b.Id);
                
                CollectionUpdater.UpdateCollection(
                    EventsForSelectedDate,
                    visibleEvents,
                    (a, b) => a.Id == b.Id);

                // Храним полный список для расчёта прогресса и возможных деталей
                _allEventsForSelectedDate = eventsList;

                // Обновляем прогресс только если изменения значительны
                if (shouldUpdateProgress)
                {
                    CompletionProgress = prog;
                    CompletionPercentage = percent;
                }

                // Логируем актуальное количество после обновления
                _logger.LogDebug("UpdateUIWithEvents: Updated FlattenedEvents with {Count} events for date {Date}", FlattenedEvents.Count, SelectedDate.ToShortDateString());

                _logger?.LogInformation("UI collections updated: Remaining cards={Count}", FlattenedEvents.Count);
                _logger?.LogInformation("Progress now {Percent}% ({Progress:P2})", CompletionPercentage, CompletionProgress);
            });
            
            // Показываем конфетти вне UI потока если нужно
            if (shouldShowConfetti)
            {
                _logger.LogInformation("All tasks completed! Showing confetti animation");
                _ = Task.Run(async () =>
                {
                    // Настраиваем производительность в зависимости от платформы
#if ANDROID
                    _confettiManager.ConfigurePerformance(ConfettiPerformanceLevel.Low);
#else
                    _confettiManager.ConfigurePerformance(ConfettiPerformanceLevel.Medium);
#endif
                    await _confettiManager.ShowConfettiAsync(duration: 3000, particleCount: 60);
                });
            }
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
            await MauiApp.Current.MainPage.DisplayAlert(
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
                var counts = await _queryBus.SendAsync<Dictionary<DateTime, Dictionary<EventType, int>>>(
                    new GetEventCountsForDatesQuery(CalendarDays.ToList()));

                await MauiApp.Current.MainPage.Dispatcher.DispatchAsync(() =>
                {
                    EventCountsByDate = counts;
                    OnPropertyChanged(nameof(EventCountsByDate));
                });

                _logger.LogInformation("LoadEventCountsForVisibleDaysAsync completed. Processed {DateCount} dates", counts.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LoadEventCountsForVisibleDaysAsync");
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
        
        public async Task ToggleEventCompletionAsync(CalendarEvent? calendarEvent)
        {
            _logger?.LogInformation("🔥 ToggleEventCompletionAsync CALLED! Event: {EventTitle}", calendarEvent?.Title ?? "null");

            if (calendarEvent == null)
            {
                _logger?.LogWarning("ToggleEventCompletionAsync called with null CalendarEvent");
                return;
            }

            // Original logic from here
            _logger?.LogInformation("Successfully received CalendarEvent. Event: {EventTitle}", calendarEvent.Title);

            // Разрешаем отмечать выполненными задачи за прошедшие даты и сегодня;
            // запрещаем только будущие события (SelectedDate > Today)
            if (SelectedDate.Date > DateTime.Today)
            {
                _logger?.LogWarning("Attempt to complete a future event – operation aborted");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await MauiApp.Current.MainPage.DisplayAlert("Недоступно", "Задачи из будущего отмечать нельзя", "OK");
                });
                return;
            }
            if (calendarEvent != null)
            {
                try
                {
                    // Capture original state for rollback
                    var originalState = calendarEvent.IsCompleted;
                    _logger?.LogDebug("Original IsCompleted state for event {EventId}: {State}", calendarEvent.Id, originalState);
                    
                    try
                    {
                        // Toggle state first
                        calendarEvent.IsCompleted = !calendarEvent.IsCompleted;
                        _logger?.LogDebug("Toggled IsCompleted for event {EventId} to {State}", calendarEvent.Id, calendarEvent.IsCompleted);
                        
                        // Update the IsCompleted state in the master list _allEventsForSelectedDate
                        var eventInMasterList = _allEventsForSelectedDate?.FirstOrDefault(e => e.Id == calendarEvent.Id);
                        if (eventInMasterList != null)
                        {
                            eventInMasterList.IsCompleted = calendarEvent.IsCompleted;
                            _logger?.LogDebug("Updated IsCompleted for event {EventId} in _allEventsForSelectedDate to {State}", eventInMasterList.Id, eventInMasterList.IsCompleted);
                        }
                        else
                        {
                            _logger?.LogWarning("Event {EventId} not found in _allEventsForSelectedDate during toggle. Progress might be inaccurate.", calendarEvent.Id);
                        }
                        
                        // Persist via CQRS command handler
                        await _commandBus.SendAsync(new CalendarCommands.ToggleEventCompletionCommand(calendarEvent.Id));

                        // Update cache if needed
                        if (TryGetCache(SelectedDate.Date, out var cachedEvents, out _))
                        {
                            // Update cache list in service
                            var eventToUpdate = cachedEvents.FirstOrDefault(e => e.Id == calendarEvent.Id);
                            if (eventToUpdate != null)
                            {
                                eventToUpdate.IsCompleted = calendarEvent.IsCompleted;
                                SetCache(SelectedDate.Date, cachedEvents);
                                _logger?.LogDebug("Cache updated for event {EventId}", calendarEvent.Id);
                            }
                        }
                        
                        // Update overdue events counter if needed
                        if (calendarEvent.Date.Date < DateTime.Today)
                        {
                            await CheckOverdueEventsAsync();
                        }

                        // Immediately recalculate progress and notify UI
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            // Удаляем или возвращаем карточку без пересоздания коллекций
                            if (calendarEvent.IsCompleted)
                            {
                                FlattenedEvents.Remove(calendarEvent);
                                SortedEvents.Remove(calendarEvent);
                                EventsForSelectedDate.Remove(calendarEvent);
                            }
                            else
                            {
                                // вернули невыполненной – вставляем в нужное место
                                int insert = SortedEvents.TakeWhile(e => e.Date.TimeOfDay < calendarEvent.Date.TimeOfDay).Count();
                                FlattenedEvents.Add(calendarEvent);
                                SortedEvents.Insert(insert, calendarEvent);
                                EventsForSelectedDate.Add(calendarEvent);
                            }

                            // Recalculate progress using the updated _allEventsForSelectedDate
                            var (prog, percent) = _progressCalculator.CalculateProgress(_allEventsForSelectedDate);
                            CompletionProgress = prog;
                            CompletionPercentage = percent;
                            OnPropertyChanged(nameof(CompletionProgress));
                            OnPropertyChanged(nameof(CompletionPercentage));
                        });
                    }
                    catch (Exception ex)
                    {
                        // Rollback on error
                        _logger.LogError(ex, "Error toggling event completion");
                        calendarEvent.IsCompleted = originalState;
                        
                        await MauiApp.Current.MainPage.Dispatcher.DispatchAsync(async () =>
                        {
                            await MauiApp.Current.MainPage.DisplayAlert(
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
            _logger?.LogInformation("SelectDateAsync invoked with {SelectedDate}", date);
            if (date.Date == SelectedDate.Date)
            {
                _logger.LogDebug("SelectDateAsync: Same date selected, ignoring");
                return;
            }

#if DEBUG
            Debug.WriteLine($"SelectDateAsync called with date: {date.ToShortDateString()}");
            Debug.WriteLine($"Current SelectedDate before change: {SelectedDate.ToShortDateString()}");
#endif

            // Сохраняем старую дату для возможного отката
            var previousDate = SelectedDate;

            try
            {
                // Обновляем дату - setter уже вызовет OnPropertyChanged и LoadTodayEventsAsync через дебаунсер
                SelectedDate = date;

                // Сохраняем выбранную дату в настройках
                SaveSelectedDate(date);

#if DEBUG
                Debug.WriteLine($"SelectedDate after change: {SelectedDate.ToShortDateString()}");
#endif
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error selecting date {Date}", date.ToShortDateString());

                // Возвращаем предыдущую дату в случае ошибки
                if (previousDate != date)
                {
                    SelectedDate = previousDate;
                }
            }
        }
        
        private async Task OpenMonthCalendarAsync(DateTime date)
        {
            // Сохраняем выбранную дату перед переходом
            SelectedDate = date;
            
            // Вместо навигации к несуществующей странице покажем сообщение
            await MauiApp.Current.MainPage.DisplayAlert(
                "Календарь", 
                $"Полный календарь для даты {date:dd.MM.yyyy} находится в разработке",
                "OK");
        }
        
        // Переход к детальной странице события
        private async Task ViewEventDetailsAsync(CalendarEvent calendarEvent)
        {
            _logger?.LogInformation("👆 ViewEventDetailsAsync CALLED! Event: {EventTitle} (ID: {EventId})", calendarEvent?.Title ?? "null", calendarEvent?.Id);

            if (calendarEvent == null)
            {
                _logger?.LogWarning("ViewEventDetailsAsync called with null CalendarEvent");
                return;
            }
                
            try
            {
                // Показываем детали события в простом диалоге (временно, пока нет детальной страницы)
                var details = $"Событие: {calendarEvent.Title}\n" +
                             $"Описание: {calendarEvent.Description ?? "Нет описания"}\n" +
                             $"Дата: {calendarEvent.Date:dd.MM.yyyy HH:mm}\n" +
                             $"Тип: {calendarEvent.EventType}\n" +
                             $"Статус: {(calendarEvent.IsCompleted ? "Выполнено" : "Не выполнено")}";

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await MauiApp.Current.MainPage.DisplayAlert(
                        "Детали события",
                        details,
                        "OK"
                    );
                });

                _logger?.LogInformation("Event details dialog shown for event: {EventTitle}", calendarEvent.Title);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error showing event details for event: {EventTitle}", calendarEvent?.Title);
                
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await MauiApp.Current.MainPage.DisplayAlert(
                        "Ошибка",
                        "Не удалось показать детали события",
                        "OK"
                    );
                });
            }
        }
        
        // Отложить событие на более позднее время
        private async Task PostponeEventAsync(CalendarEvent calendarEvent)
        {
            if (calendarEvent == null)
                return;
                
            // В реальном приложении здесь был бы код для открытия диалога выбора нового времени
            // и сохранения измененного события
            
            // Пример отложения события на день (заглушка для демонстрации):
            await MauiApp.Current.MainPage.DisplayActionSheet(
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
                string savedDateString = Preferences.Get(SelectedDateKey, string.Empty);
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
                
                var today = DateTime.Today;
                
                // Проверяем, если уже на сегодняшней дате - не обновляем
                if (SelectedDate.Date == today.Date && VisibleDate.Date == today.Date)
                {
                    _logger.LogDebug("Already on today's date, skipping update");
                    return;
                }
                
                // Обновляем даты
                SelectedDate = today;
                VisibleDate = today;
                
                // Загрузка задач произойдёт после завершения анимации прокрутки (ScrollIdleNotifierBehavior)
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
#if DEBUG
                Debug.WriteLine($"Error loading initial data: {ex.Message}");
#endif
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
                var fetchedRestrictions = await _queryBus.SendAsync<IReadOnlyList<RestrictionInfo>>(new GetActiveRestrictionsQuery());
                _logger.LogInformation("Fetched {Count} potential restrictions.", fetchedRestrictions?.Count ?? 0);

                // Use Dispatcher for UI collection modification safety
                await MauiApp.Current.MainPage.Dispatcher.DispatchAsync(() =>
                {
                    ActiveRestrictions.Clear();
                    var today = DateTime.Today;
                    int validRestrictionsCount = 0;

                    if (fetchedRestrictions != null)
                    {
                        var categoryDict = new Dictionary<string, RestrictionInfo>();

                        foreach (var restriction in fetchedRestrictions)
                        {
                            var (glyph, description) = GetRestrictionUIDetails(restriction.OriginalType);
                            if (string.IsNullOrEmpty(glyph))
                            {
                                // try keyword mapping from description
                                (glyph, description) = GetRestrictionGlyph(restriction.Description);
                            }

                            if (string.IsNullOrEmpty(glyph)) continue;

                            // Deduplicate by description; keep the shortest remaining days
                            if (categoryDict.TryGetValue(description, out var existing))
                            {
                                if (restriction.RemainingDays < existing.RemainingDays)
                                {
                                    existing.RemainingDays = restriction.RemainingDays;
                                    existing.EndDate = restriction.EndDate;
                                }
                            }
                            else
                            {
                                restriction.IconGlyph = glyph;
                                restriction.Description = description;
                                categoryDict[description] = restriction;
                            }
                        }

                        foreach (var info in categoryDict.Values)
                        {
                            ActiveRestrictions.Add(info);
                        }
                        validRestrictionsCount = ActiveRestrictions.Count;

                        // Update legacy single-restriction bindings using first restriction (if any)
                        var first = ActiveRestrictions.FirstOrDefault();
                        if (first != null)
                        {
                            RestrictionIcon = first.IconGlyph;
                            CurrentRestrictionText = first.Description;
                            // Background accent – red if ≤3 days, else neutral subtle gray
                            RestrictionBackgroundColor = first.RemainingDays <= 3
                                ? Color.FromArgb("#FFFFE5E5") // light red tint
                                : Color.FromArgb("#FFF0F0F0");
                        }
                        else
                        {
                            RestrictionIcon = string.Empty;
                            CurrentRestrictionText = string.Empty;
                            RestrictionBackgroundColor = Colors.Transparent;
                        }
                    }

                    HasActiveRestriction = ActiveRestrictions.Any();
                    _logger.LogInformation("Processed restrictions. Added {Count} valid restrictions to UI collection. HasActiveRestriction: {HasActive}", 
                                            validRestrictionsCount, HasActiveRestriction);
                });
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error checking active restrictions");
                 await MauiApp.Current.MainPage.Dispatcher.DispatchAsync(() => 
                 {
                     ActiveRestrictions.Clear();
                     HasActiveRestriction = false;
                 });
            }
        }

        // Helper method for mapping EventType to UI details
        private (string Icon, string Description) GetRestrictionUIDetails(EventType eventType)
        {
            // TODO: Define actual icons and short descriptions based on EventType values
            switch (eventType)
            {
                // Example mappings (replace with actual Material Icon glyphs and short names)
                case EventType.CriticalWarning: // Assuming this could be a general restriction
                    return ("\ue002", "Warning"); // warning
                case EventType.MedicalVisit:
                     return ("\ue87d", "Visit"); // medical_services
                // Add specific restriction types if they exist in EventType enum
                // case EventType.NoSport:
                //     return ("\ue52f", "Спорт"); // directions_run
                // case EventType.NoWater:
                //     return ("\ue1a1", "Вода"); // water_drop
                // case EventType.NoHaircut:
                //     return ("\ue530", "Стрижка"); // content_cut
                default:
                    _logger.LogWarning("No UI mapping defined for EventType: {EventType}", eventType);
                    return (string.Empty, string.Empty); // No icon/desc for unknown/unmapped types
            }
        }

        // New helper: map by title keyword (simple heuristic)
        private (string Icon, string Description) GetRestrictionGlyph(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return (string.Empty, string.Empty);

            title = title.ToLowerInvariant();

            // Map keywords to image file names located in Resources/AppIcon/
            if (title.Contains("курен")) return ("\uebc3", "Smoking");          // smoking rooms
            if (title.Contains("алкогол")) return ("\ueadf", "Alcohol");         // liquor
            if (title.Contains("спорт")) return ("\ue566", "Sport");           // fitness_center
            if (title.Contains("загар")) return ("\ue430", "Sun");             // wb_sunny
            if (title.Contains("45")) return ("\uebed", "Sleep45");           // airline_seat_recline_extra
            if (title.Contains("стрижк")) return ("\ue14e", "Haircut");        // content_cut
            if (title.Contains("секс")) return ("no_sex.png", "Sex");
            if (title.Contains("головн") && title.Contains("убор")) return ("no_headwear.png", "Headwear");
            if (title.Contains("потоотд") || title.Contains("пот")) return ("no_sweating.png", "Sweat");
            if (title.Contains("бассейн") || title.Contains("плаван") || title.Contains("swimm")) return ("no_swimming.png", "Swimming");
            if (title.Contains("наклон") || title.Contains("голов") && title.Contains("вниз")) return ("no_head_tilt.png", "HeadTilt");
            if (title.Contains("стрижка")) return ("no_haircut.png", "Haircut");
            if (title.Contains("стайл") || title.Contains("уклад") || title.Contains("средств")) return ("no_styling.png", "Styling");
            if (title.Contains("горизонталь") || title.Contains("лежа")) return ("no_horizontal_sleep.png", "HorizontalSleep");

            return (string.Empty, string.Empty);
        }

        #region CacheHelpers
        private bool TryGetCache(DateTime date, out List<CalendarEvent> events, out DateTimeOffset lastUpdate) =>
            _cacheService.TryGet(date, out events, out lastUpdate);

        private void SetCache(DateTime date, IEnumerable<CalendarEvent> events) => _cacheService.Set(date, events);
        #endregion

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
        public ICommand TestLongPressCommand { get; } // Removed for diagnostics

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