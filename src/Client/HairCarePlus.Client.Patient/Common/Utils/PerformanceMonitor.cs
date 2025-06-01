using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Client.Patient.Common.Utils
{
    /// <summary>
    /// Утилита для мониторинга производительности операций
    /// </summary>
    public class PerformanceMonitor
    {
        private readonly ILogger<PerformanceMonitor> _logger;
        private readonly Dictionary<string, Stopwatch> _timers = new();
        private readonly Dictionary<string, List<long>> _metrics = new();
        private readonly object _lock = new();

        public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Начинает отслеживание времени выполнения операции
        /// </summary>
        public void StartTimer(string operation)
        {
            lock (_lock)
            {
                _timers[operation] = Stopwatch.StartNew();
            }
        }

        /// <summary>
        /// Останавливает таймер и записывает результат
        /// </summary>
        public long StopTimer(string operation)
        {
            lock (_lock)
            {
                if (_timers.TryGetValue(operation, out var timer))
                {
                    timer.Stop();
                    var elapsed = timer.ElapsedMilliseconds;
                    
                    // Сохраняем метрику
                    if (!_metrics.ContainsKey(operation))
                    {
                        _metrics[operation] = new List<long>();
                    }
                    _metrics[operation].Add(elapsed);
                    
                    // Логируем результат
                    _logger.LogInformation("Operation {Operation} took {ElapsedMs}ms", operation, elapsed);
                    
                    // Предупреждаем о медленных операциях
                    if (elapsed > 100)
                    {
                        _logger.LogWarning("Slow operation detected: {Operation} took {ElapsedMs}ms", operation, elapsed);
                    }
                    
                    _timers.Remove(operation);
                    return elapsed;
                }
                
                _logger.LogWarning("Timer for operation {Operation} was not found", operation);
                return -1;
            }
        }

        /// <summary>
        /// Получает среднее время выполнения операции
        /// </summary>
        public double GetAverageTime(string operation)
        {
            lock (_lock)
            {
                if (_metrics.TryGetValue(operation, out var times) && times.Count > 0)
                {
                    var sum = 0L;
                    foreach (var time in times)
                    {
                        sum += time;
                    }
                    return (double)sum / times.Count;
                }
                return 0;
            }
        }

        /// <summary>
        /// Сбрасывает все метрики
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _timers.Clear();
                _metrics.Clear();
            }
        }

        /// <summary>
        /// Выводит сводку по всем операциям
        /// </summary>
        public void LogSummary()
        {
            lock (_lock)
            {
                _logger.LogInformation("=== Performance Summary ===");
                foreach (var kvp in _metrics)
                {
                    var operation = kvp.Key;
                    var times = kvp.Value;
                    if (times.Count > 0)
                    {
                        var avg = GetAverageTime(operation);
                        var min = times[0];
                        var max = times[0];
                        
                        foreach (var time in times)
                        {
                            if (time < min) min = time;
                            if (time > max) max = time;
                        }
                        
                        _logger.LogInformation(
                            "Operation: {Operation} | Count: {Count} | Avg: {Avg:F2}ms | Min: {Min}ms | Max: {Max}ms",
                            operation, times.Count, avg, min, max);
                    }
                }
                _logger.LogInformation("========================");
            }
        }
    }
} 