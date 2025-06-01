using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using HairCarePlus.Client.Patient.Common.Utils;
using Microsoft.Extensions.Logging;
using HairCarePlus.Shared.Common.CQRS;
using HairCarePlus.Client.Patient.Features.Calendar.Application.Queries;
using HairCarePlus.Client.Patient.Features.Calendar.Models;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services.Implementation
{
    /// <summary>
    /// Реализация сервиса предзагрузки данных календаря
    /// </summary>
    public class PreloadingService : IPreloadingService
    {
        private readonly ICalendarCacheService _cacheService;
        private readonly IQueryBus _queryBus;
        private readonly ILogger<PreloadingService> _logger;
        private readonly PerformanceMonitor _performanceMonitor;
        
        private readonly ConcurrentQueue<DateTime> _preloadQueue = new();
        private readonly SemaphoreSlim _preloadSemaphore = new(3); // Максимум 3 параллельные загрузки
        private CancellationTokenSource? _backgroundCts;
        private Task? _backgroundTask;
        
        // Настройки предзагрузки
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(1);
        private readonly int _maxQueueSize = 50;
        private readonly int _batchSize = 5;
        
        public PreloadingService(
            ICalendarCacheService cacheService,
            IQueryBus queryBus,
            ILogger<PreloadingService> logger,
            PerformanceMonitor performanceMonitor)
        {
            _cacheService = cacheService;
            _queryBus = queryBus;
            _logger = logger;
            _performanceMonitor = performanceMonitor;
        }
        
        public async Task PreloadAdjacentDatesAsync(DateTime centerDate, int daysBefore = 3, int daysAfter = 3, CancellationToken cancellationToken = default)
        {
            _performanceMonitor.StartTimer("PreloadAdjacentDates");
            
            try
            {
                var datesToPreload = new List<DateTime>();
                
                // Добавляем даты до центральной
                for (int i = daysBefore; i > 0; i--)
                {
                    datesToPreload.Add(centerDate.AddDays(-i));
                }
                
                // Добавляем даты после центральной
                for (int i = 1; i <= daysAfter; i++)
                {
                    datesToPreload.Add(centerDate.AddDays(i));
                }
                
                // Фильтруем уже закэшированные даты
                var datesToLoad = datesToPreload.Where(date => 
                {
                    if (_cacheService.TryGet(date, out _, out var lastUpdate))
                    {
                        return (DateTimeOffset.Now - lastUpdate) > _cacheExpiration;
                    }
                    return true;
                }).ToList();
                
                if (datesToLoad.Any())
                {
                    _logger.LogInformation("Preloading {Count} dates around {CenterDate}", datesToLoad.Count, centerDate);
                    
                    // Загружаем параллельно с ограничением
                    var tasks = datesToLoad.Select(date => PreloadDateAsync(date, cancellationToken));
                    await Task.WhenAll(tasks);
                }
                else
                {
                    _logger.LogDebug("All adjacent dates for {CenterDate} are already cached", centerDate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preloading adjacent dates for {CenterDate}", centerDate);
            }
            finally
            {
                _performanceMonitor.StopTimer("PreloadAdjacentDates");
            }
        }
        
        public async Task PreloadDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            _performanceMonitor.StartTimer("PreloadDateRange");
            
            try
            {
                var totalDays = (endDate - startDate).Days + 1;
                if (totalDays <= 0 || totalDays > 365)
                {
                    _logger.LogWarning("Invalid date range for preloading: {StartDate} to {EndDate}", startDate, endDate);
                    return;
                }
                
                _logger.LogInformation("Preloading date range from {StartDate} to {EndDate} ({TotalDays} days)", 
                    startDate, endDate, totalDays);
                
                // Разбиваем на батчи для параллельной загрузки
                var dates = Enumerable.Range(0, totalDays)
                    .Select(i => startDate.AddDays(i))
                    .ToList();
                
                foreach (var batch in dates.Chunk(_batchSize))
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                        
                    var tasks = batch.Select(date => PreloadDateAsync(date, cancellationToken));
                    await Task.WhenAll(tasks);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preloading date range");
            }
            finally
            {
                _performanceMonitor.StopTimer("PreloadDateRange");
            }
        }
        
        public async Task StartBackgroundPreloadingAsync(CancellationToken cancellationToken = default)
        {
            if (_backgroundTask != null && !_backgroundTask.IsCompleted)
            {
                _logger.LogDebug("Background preloading is already running");
                return;
            }
            
            _backgroundCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _backgroundTask = BackgroundPreloadingLoop(_backgroundCts.Token);
            
            _logger.LogInformation("Background preloading started");
        }
        
        public void StopBackgroundPreloading()
        {
            _backgroundCts?.Cancel();
            _backgroundCts?.Dispose();
            _backgroundCts = null;
            
            _logger.LogInformation("Background preloading stopped");
        }
        
        public void ClearPreloadQueue()
        {
            while (_preloadQueue.TryDequeue(out _))
            {
                // Очищаем очередь
            }
            
            _logger.LogDebug("Preload queue cleared");
        }
        
        /// <summary>
        /// Добавляет дату в очередь предзагрузки
        /// </summary>
        public void QueueDateForPreload(DateTime date)
        {
            // Проверяем размер очереди
            if (_preloadQueue.Count >= _maxQueueSize)
            {
                _logger.LogDebug("Preload queue is full, skipping date {Date}", date);
                return;
            }
            
            // Проверяем, не находится ли дата уже в очереди
            if (!_preloadQueue.Contains(date))
            {
                _preloadQueue.Enqueue(date);
                _logger.LogDebug("Date {Date} queued for preloading", date);
            }
        }
        
        private async Task PreloadDateAsync(DateTime date, CancellationToken cancellationToken)
        {
            await _preloadSemaphore.WaitAsync(cancellationToken);
            
            try
            {
                // Проверяем кэш перед загрузкой
                if (_cacheService.TryGet(date, out _, out var lastUpdate) &&
                    (DateTimeOffset.Now - lastUpdate) <= _cacheExpiration)
                {
                    _logger.LogDebug("Date {Date} is already cached, skipping preload", date);
                    return;
                }
                
                _logger.LogDebug("Preloading events for {Date}", date);
                
                // Загружаем события
                var events = await _queryBus.SendAsync<IEnumerable<CalendarEvent>>(
                    new GetEventsForDateQuery(date), cancellationToken);
                
                // Сохраняем в кэш
                _cacheService.Set(date, events);
                
                _logger.LogDebug("Successfully preloaded {Count} events for {Date}", 
                    events?.Count() ?? 0, date);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Preloading cancelled for {Date}", date);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preloading date {Date}", date);
            }
            finally
            {
                _preloadSemaphore.Release();
            }
        }
        
        private async Task BackgroundPreloadingLoop(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Background preloading loop started");
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Ждем немного перед следующей итерацией
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    
                    // Обрабатываем очередь
                    var processedCount = 0;
                    while (_preloadQueue.TryDequeue(out var date) && processedCount < _batchSize)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;
                            
                        await PreloadDateAsync(date, cancellationToken);
                        processedCount++;
                    }
                    
                    if (processedCount > 0)
                    {
                        _logger.LogDebug("Processed {Count} dates from preload queue", processedCount);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in background preloading loop");
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
            }
            
            _logger.LogInformation("Background preloading loop stopped");
        }
    }
} 