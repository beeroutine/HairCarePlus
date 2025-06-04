using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using HairCarePlus.Client.Patient.Features.Calendar.Application.Commands;
using HairCarePlus.Client.Patient.Features.Calendar.Application.Queries;
using HairCarePlus.Client.Patient.Features.Calendar.Messages;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using HairCarePlus.Client.Patient.Infrastructure.Services.Interfaces;
using HairCarePlus.Client.Patient.ViewModels;
using HairCarePlus.Shared.Common.CQRS;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Graphics;
using MauiApplication = Microsoft.Maui.Controls.Application;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    /// <summary>
    /// Reactive implementation of TodayViewModel using ReactiveUI patterns
    /// </summary>
    public class TodayViewModelReactive : ReactiveBaseViewModel
    {
        private readonly ICalendarService _calendarService;
        private readonly ICalendarCacheService _cacheService;
        private readonly ICalendarLoader _eventLoader;
        private readonly IProgressCalculator _progressCalculator;
        private readonly ICommandBus _commandBus;
        private readonly IQueryBus _queryBus;
        private readonly IMessenger _messenger;
        private readonly IProfileService _profileService;
        
        private readonly SourceCache<CalendarEvent, Guid> _eventsCache;
        private readonly ReadOnlyObservableCollection<CalendarEvent> _flattenedEvents;
        private readonly ReadOnlyObservableCollection<DateTime> _calendarDays;
        
        #region Reactive Properties
        
        [Reactive] public DateTime SelectedDate { get; set; }
        [Reactive] public DateTime VisibleDate { get; set; }
        [Reactive] public bool IsLoading { get; set; }
        [Reactive] public bool IsRefreshing { get; set; }
        [Reactive] public double CompletionProgress { get; set; }
        [Reactive] public int CompletionPercentage { get; set; }
        [Reactive] public bool HasActiveRestriction { get; set; }
        [Reactive] public string CurrentMonthName { get; set; }
        [Reactive] public string DaysSinceTransplantSubtitle { get; set; }
        
        #endregion
        
        #region Observables
        
        private readonly ObservableAsPropertyHelper<string> _formattedSelectedDate;
        public string FormattedSelectedDate => _formattedSelectedDate.Value;
        
        private readonly ObservableAsPropertyHelper<int> _todayDay;
        public int TodayDay => _todayDay.Value;
        
        private readonly ObservableAsPropertyHelper<string> _todayDayOfWeek;
        public string TodayDayOfWeek => _todayDayOfWeek.Value;
        
        #endregion
        
        #region Collections
        
        public ReadOnlyObservableCollection<CalendarEvent> FlattenedEvents => _flattenedEvents;
        public ReadOnlyObservableCollection<DateTime> CalendarDays => _calendarDays;
        public ObservableCollection<RestrictionInfo> ActiveRestrictions { get; }
        
        #endregion
        
        #region Commands
        
        public ReactiveCommand<Unit, Unit> GoToTodayCommand { get; private set; }
        public ReactiveCommand<DateTime, Unit> SelectDateCommand { get; private set; }
        public ReactiveCommand<CalendarEvent, Unit> ToggleEventCompletionCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> RefreshCommand { get; private set; }
        public ReactiveCommand<CalendarEvent, Unit> ShowEventDetailsCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> LoadMoreDatesCommand { get; private set; }
        
        #endregion
        
        public TodayViewModelReactive(
            ICalendarService calendarService,
            ICalendarCacheService cacheService,
            ICalendarLoader eventLoader,
            IProgressCalculator progressCalculator,
            ILogger<TodayViewModelReactive> logger,
            ICommandBus commandBus,
            IQueryBus queryBus,
            IMessenger messenger,
            IProfileService profileService) : base(logger)
        {
            _calendarService = calendarService;
            _cacheService = cacheService;
            _eventLoader = eventLoader;
            _progressCalculator = progressCalculator;
            _commandBus = commandBus;
            _queryBus = queryBus;
            _messenger = messenger;
            _profileService = profileService;
            
            Title = "Today";
            SelectedDate = DateTime.Today;
            VisibleDate = DateTime.Today;
            ActiveRestrictions = new ObservableCollection<RestrictionInfo>();
            
            // Initialize caches
            _eventsCache = new SourceCache<CalendarEvent, Guid>(e => e.Id);
            
            // Create observables
            _formattedSelectedDate = this
                .WhenAnyValue(x => x.SelectedDate)
                .Select(date => date.ToString("ddd, MMM d"))
                .ToProperty(this, x => x.FormattedSelectedDate);
                
            _todayDay = Observable
                .Timer(TimeSpan.Zero, TimeSpan.FromHours(1))
                .Select(_ => DateTime.Today.Day)
                .ToProperty(this, x => x.TodayDay);
                
            _todayDayOfWeek = Observable
                .Timer(TimeSpan.Zero, TimeSpan.FromHours(1))
                .Select(_ => DateTime.Today.ToString("ddd"))
                .ToProperty(this, x => x.TodayDayOfWeek);
            
            // Calendar days generation
            var calendarDaysSource = new SourceList<DateTime>();
            GenerateCalendarDays(calendarDaysSource);
            calendarDaysSource
                .Connect()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _calendarDays)
                .Subscribe();
            
            // Events filtering and binding
            _eventsCache
                .Connect()
                .Filter(e => !e.IsCompleted && e.EventType != EventType.CriticalWarning)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _flattenedEvents)
                .Subscribe();
            
            // Create commands
            CreateCommands();
            
            // Setup reactive pipelines
            SetupReactivePipelines();
        }
        
        protected override void HandleActivation(CompositeDisposable disposables)
        {
            base.HandleActivation(disposables);
            
            // Selected date change triggers event loading
            this.WhenAnyValue(x => x.SelectedDate)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .DistinctUntilChanged()
                .SelectMany(date => Observable.FromAsync(() => LoadEventsForDateAsync(date)))
                .Subscribe()
                .DisposeWith(disposables);
            
            // Visible date updates month name
            this.WhenAnyValue(x => x.VisibleDate)
                .Select(date => date.ToString("MMMM"))
                .BindTo(this, x => x.CurrentMonthName)
                .DisposeWith(disposables);
            
            // Days since transplant calculation
            this.WhenAnyValue(x => x.SelectedDate)
                .Select(date => $"Day {(date.Date - _profileService.SurgeryDate.Date).Days + 1} post hair transplant")
                .BindTo(this, x => x.DaysSinceTransplantSubtitle)
                .DisposeWith(disposables);
            
            // Progress calculation from events
            _eventsCache
                .Connect()
                .Throttle(TimeSpan.FromMilliseconds(100))
                .Select(_ => CalculateProgress())
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(progress =>
                {
                    CompletionProgress = progress.Item1;
                    CompletionPercentage = progress.Item2;
                })
                .DisposeWith(disposables);
            
            // Handle event updated messages
            _messenger.Register<EventUpdatedMessage>(this, async (recipient, message) =>
            {
                Logger?.LogDebug("Received EventUpdatedMessage for {EventId}", message.Value);
                await LoadEventsForDateAsync(SelectedDate);
            });
            
            // Initial data load
            Observable.StartAsync(LoadInitialDataAsync)
                .Subscribe()
                .DisposeWith(disposables);
        }
        
        private void CreateCommands()
        {
            // GoToToday with throttling
            GoToTodayCommand = ReactiveCommand.CreateFromTask(
                async () =>
                {
                    var today = DateTime.Today;
                    if (SelectedDate.Date != today)
                    {
                        SelectedDate = today;
                        VisibleDate = today;
                    }
                    await Task.CompletedTask;
                },
                outputScheduler: RxApp.MainThreadScheduler);
            
            // Throttle GoToToday to prevent rapid taps
            GoToTodayCommand
                .Throttle(TimeSpan.FromMilliseconds(500))
                .InvokeCommand(GoToTodayCommand);
            
            // Select date command
            SelectDateCommand = ReactiveCommand.CreateFromTask<DateTime>(
                async date =>
                {
                    if (date.Date != SelectedDate.Date)
                    {
                        SelectedDate = date.Date;
                        if (date.Month != VisibleDate.Month || date.Year != VisibleDate.Year)
                        {
                            VisibleDate = date;
                        }
                    }
                    await Task.CompletedTask;
                });
            
            // Toggle event completion
            var canToggleEvent = this
                .WhenAnyValue(x => x.SelectedDate)
                .Select(date => date.Date == DateTime.Today);
                
            ToggleEventCompletionCommand = ReactiveCommand.CreateFromTask<CalendarEvent>(
                async calendarEvent =>
                {
                    if (calendarEvent == null) return;
                    
                    try
                    {
                        // Toggle completion
                        calendarEvent.IsCompleted = !calendarEvent.IsCompleted;
                        
                        // Send command
                        await _commandBus.SendAsync(new ToggleEventCompletionCommand(calendarEvent.Id));
                        
                        // Update cache
                        _eventsCache.AddOrUpdate(calendarEvent);
                        
                        // Update cache service
                        UpdateCacheForDate(SelectedDate);
                    }
                    catch (Exception ex)
                    {
                        Logger?.LogError(ex, "Error toggling event completion");
                        // Rollback
                        calendarEvent.IsCompleted = !calendarEvent.IsCompleted;
                        _eventsCache.AddOrUpdate(calendarEvent);
                    }
                },
                canToggleEvent);
            
            // Refresh command
            RefreshCommand = ReactiveCommand.CreateFromTask(
                async () =>
                {
                    IsRefreshing = true;
                    try
                    {
                        await LoadEventsForDateAsync(SelectedDate);
                        await LoadActiveRestrictionsAsync();
                    }
                    finally
                    {
                        IsRefreshing = false;
                    }
                },
                this.WhenAnyValue(x => x.IsRefreshing).Select(x => !x));
            
            // Show event details
            ShowEventDetailsCommand = ReactiveCommand.CreateFromTask<CalendarEvent>(
                async calendarEvent =>
                {
                    if (calendarEvent != null)
                    {
                        await MauiApplication.Current.MainPage.DisplayAlert(
                            calendarEvent.Title,
                            calendarEvent.Description,
                            "OK");
                    }
                });
            
            // Load more dates
            LoadMoreDatesCommand = ReactiveCommand.CreateFromTask(
                LoadMoreDatesAsync,
                this.WhenAnyValue(x => x.IsLoading).Select(x => !x));
        }
        
        private void SetupReactivePipelines()
        {
            // Auto-refresh every 5 minutes when app is active
            Observable
                .Timer(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5))
                .Where(_ => !IsRefreshing)
                .SelectMany(_ => RefreshCommand.Execute())
                .Subscribe();
        }
        
        private async Task LoadInitialDataAsync()
        {
            try
            {
                IsLoading = true;
                await LoadEventsForDateAsync(SelectedDate);
                await LoadActiveRestrictionsAsync();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error loading initial data");
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private async Task LoadEventsForDateAsync(DateTime date)
        {
            try
            {
                // Check cache first
                if (_cacheService.TryGet(date, out var cachedEvents, out var lastUpdate) &&
                    (DateTimeOffset.Now - lastUpdate) <= TimeSpan.FromMinutes(1))
                {
                    Logger?.LogInformation("Using cached events for {Date}", date);
                    UpdateEventsCache(cachedEvents);
                    return;
                }
                
                // Load from service
                var events = (await _queryBus.SendAsync<IEnumerable<CalendarEvent>>(
                    new GetEventsForDateQuery(date))).ToList();
                
                // Update caches
                UpdateEventsCache(events);
                _cacheService.Set(date, events);
                
                Logger?.LogInformation("Loaded {Count} events for {Date}", events.Count, date);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error loading events for {Date}", date);
            }
        }
        
        private void UpdateEventsCache(IEnumerable<CalendarEvent> events)
        {
            _eventsCache.Edit(updater =>
            {
                updater.Clear();
                updater.AddOrUpdate(events);
            });
        }
        
        private (double, int) CalculateProgress()
        {
            var allEvents = _eventsCache.Items.ToList();
            if (!allEvents.Any()) return (0, 0);
            
            var completed = allEvents.Count(e => e.IsCompleted);
            var total = allEvents.Count;
            
            var progress = (double)completed / total;
            var percentage = (int)(progress * 100);
            
            return (progress, percentage);
        }
        
        private void UpdateCacheForDate(DateTime date)
        {
            var events = _eventsCache.Items.ToList();
            _cacheService.Set(date, events);
        }
        
        private async Task LoadActiveRestrictionsAsync()
        {
            try
            {
                var restrictions = await _queryBus.SendAsync<IReadOnlyList<RestrictionInfo>>(
                    new GetActiveRestrictionsQuery());
                
                await MauiApplication.Current.Dispatcher.DispatchAsync(() =>
                {
                    ActiveRestrictions.Clear();
                    foreach (var restriction in restrictions ?? Enumerable.Empty<RestrictionInfo>())
                    {
                        ActiveRestrictions.Add(restriction);
                    }
                    HasActiveRestriction = ActiveRestrictions.Any();
                });
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error loading active restrictions");
            }
        }
        
        private void GenerateCalendarDays(ISourceList<DateTime> source)
        {
            const int preDays = 30;
            const int futureDays = 365;
            
            var startDate = DateTime.Today.AddDays(-preDays);
            var days = Enumerable.Range(0, preDays + futureDays)
                .Select(i => startDate.AddDays(i))
                .ToList();
                
            source.AddRange(days);
        }
        
        private async Task LoadMoreDatesAsync()
        {
            // Implementation for loading more dates
            await Task.CompletedTask;
        }
    }
} 