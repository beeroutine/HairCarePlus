using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services
{
    public interface ICalendarLoader
    {
        Task<List<CalendarEvent>> LoadEventsForDateAsync(DateTime date, CancellationToken cancellationToken = default);
    }

    public sealed class CalendarLoaderService : ICalendarLoader
    {
        private readonly ICalendarService _calendarService;
        private readonly ILogger<CalendarLoaderService> _logger;
        private const int MaxRetryAttempts = 3;
        private static readonly int[] RetryDelays = { 1000, 2000, 4000 }; // ms

        public CalendarLoaderService(ICalendarService calendarService, ILogger<CalendarLoaderService> logger)
        {
            _calendarService = calendarService;
            _logger = logger;
        }

        public async Task<List<CalendarEvent>> LoadEventsForDateAsync(DateTime date, CancellationToken cancellationToken = default)
        {
            List<CalendarEvent> events = null;
            Exception lastException = null;

            for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException(cancellationToken);
                    }

                    if (attempt > 0)
                    {
                        _logger.LogInformation("Retry {Attempt}/{Max} loading events for {Date}", attempt + 1, MaxRetryAttempts, date.ToShortDateString());
                        await Task.Delay(RetryDelays[attempt - 1], cancellationToken);
                    }

                    events = (await _calendarService.GetEventsForDateAsync(date)).ToList();
                    return events;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning(ex, "Error loading events attempt {Attempt} for {Date}", attempt + 1, date.ToShortDateString());
                }
            }

            _logger.LogError(lastException, "Failed to load events for {Date} after {Attempts} attempts", date.ToShortDateString(), MaxRetryAttempts);
            throw lastException;
        }
    }
} 