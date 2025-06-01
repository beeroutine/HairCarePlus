using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using HairCarePlus.Client.Patient.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services.Implementation
{
    public class EventAggregationServiceImpl : IEventAggregationService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<EventAggregationServiceImpl> _logger;

        public EventAggregationServiceImpl(IDbContextFactory<AppDbContext> dbContextFactory, ILogger<EventAggregationServiceImpl> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public Task<Dictionary<TimeOfDay, List<CalendarEvent>>> GroupEventsByTimeOfDayAsync(IEnumerable<CalendarEvent> events)
        {
            var result = new Dictionary<TimeOfDay, List<CalendarEvent>>
            {
                { TimeOfDay.Morning, new List<CalendarEvent>() },
                { TimeOfDay.Afternoon, new List<CalendarEvent>() },
                { TimeOfDay.Evening, new List<CalendarEvent>() }
            };

            foreach (var calendarEvent in events)
            {
                var hour = calendarEvent.Date.Hour;
                if (hour >= 5 && hour < 12)
                    result[TimeOfDay.Morning].Add(calendarEvent);
                else if (hour >= 12 && hour < 17)
                    result[TimeOfDay.Afternoon].Add(calendarEvent);
                else
                    result[TimeOfDay.Evening].Add(calendarEvent);
            }

            return Task.FromResult(result);
        }

        public Task<Dictionary<EventType, int>> GetEventCountsByTypeAsync(IEnumerable<CalendarEvent> events)
        {
            var result = new Dictionary<EventType, int>();

            foreach (EventType type in Enum.GetValues(typeof(EventType)))
                result[type] = events.Count(e => e.EventType == type);

            return Task.FromResult(result);
        }

        public async Task<Dictionary<DateTime, Dictionary<EventType, int>>> GetEventCountsByDateAndTypeAsync(IEnumerable<CalendarEvent> events, DateTime startDate, DateTime endDate)
        {
            var result = new Dictionary<DateTime, Dictionary<EventType, int>>();
            var expandedEvents = await ExpandMultiDayEventsAsync(events);

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var dateEvents = expandedEvents.Where(e => e.Date.Date <= date && (e.EndDate.HasValue ? e.EndDate.Value.Date >= date : e.Date.Date >= date)).ToList();
                var typeCounts = new Dictionary<EventType, int>();

                foreach (EventType type in Enum.GetValues(typeof(EventType)))
                {
                    typeCounts[type] = dateEvents.Count(e => e.EventType == type);
                }

                result[date.Date] = typeCounts;
            }

            return result;
        }

        public Task<IEnumerable<CalendarEvent>> ExpandMultiDayEventsAsync(IEnumerable<CalendarEvent> events)
        {
            // Implementation for expanding multi-day events if needed
            return Task.FromResult(events.ToList() as IEnumerable<CalendarEvent>);
        }

        public async Task<IEnumerable<CalendarEvent>> GetEventsForDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await GetEventsForDateRangeAsync(startDate, endDate, CancellationToken.None);
        }

        public async Task<CalendarEvent> GetEventByIdAsync(Guid id)
        {
            int maxRetries = 5;
            int retryCount = 0;
            int delay = 500;

            while (true)
            {
                try
                {
                    using var context = await _dbContextFactory.CreateDbContextAsync();
                    return await context.Events.FindAsync(id);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Cannot access a disposed object") 
                    || ex.Message.Contains("An attempt was made to use the model while it was being created"))
                {
                    if (retryCount >= maxRetries)
                    {
                        _logger?.LogError(ex, "Failed to retrieve event after {RetryCount} retries", retryCount);
                        throw;
                    }

                    _logger?.LogWarning("Failed to retrieve event due to {ErrorMessage}. Retrying in {Delay}ms. Retry attempt {RetryCount}/{MaxRetries}",
                        ex.Message, delay, retryCount + 1, maxRetries);

                    await Task.Delay(delay);
                    delay *= 2;
                    retryCount++;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error retrieving event {EventId}", id);
                    throw;
                }
            }
        }

        public async Task<Dictionary<EventType, int>> GetEventCountsByTypeAsync(DateTime date)
        {
            var events = await GetEventsForDateAsync(date);
            var result = new Dictionary<EventType, int>();

            foreach (EventType type in Enum.GetValues(typeof(EventType)))
            {
                result[type] = events.Count(e => e.EventType == type);
            }

            return result;
        }

        public async Task<IEnumerable<CalendarEvent>> GetEventsForDateAsync(DateTime date)
        {
            return await GetEventsForDateRangeAsync(date, date);
        }

        public async Task<IEnumerable<CalendarEvent>> GetEventsForDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            int maxRetries = 5;
            int retryCount = 0;
            int delay = 500;

            while (true)
            {
                try
                {
                    using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                    return await context.Events
                        .Where(e => e.Date >= startDate && e.Date <= endDate)
                        .ToListAsync(cancellationToken);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Cannot access a disposed object") 
                    || ex.Message.Contains("An attempt was made to use the model while it was being created"))
                {
                    if (retryCount >= maxRetries)
                    {
                        _logger?.LogError(ex, "Failed to retrieve events after {RetryCount} retries", retryCount);
                        throw;
                    }

                    _logger?.LogWarning("Failed to retrieve events due to {ErrorMessage}. Retrying in {Delay}ms. Retry attempt {RetryCount}/{MaxRetries}",
                        ex.Message, delay, retryCount + 1, maxRetries);

                    await Task.Delay(delay, cancellationToken);
                    delay *= 2;
                    retryCount++;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error retrieving events for date range {StartDate} to {EndDate}", startDate, endDate);
                    throw;
                }
            }
        }

        public async Task<IEnumerable<CalendarEvent>> GetActiveRestrictionsAsync()
        {
            int maxRetries = 5;
            int retryCount = 0;
            int delay = 500;

            while (true)
            {
                try
                {
                    using var context = await _dbContextFactory.CreateDbContextAsync();
                    var now = DateTime.Now;
                    var restrictions = await context.Events
                        .Where(e => e.EndDate > now && e.EventType == EventType.CriticalWarning)
                        .ToListAsync();

                    return restrictions;
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Cannot access a disposed object") || ex.Message.Contains("while it is being created"))
                {
                    if (retryCount >= maxRetries)
                    {
                        _logger?.LogError(ex, "Failed to retrieve restrictions after {RetryCount} retries", retryCount);
                        throw;
                    }

                    _logger?.LogWarning("Failed to retrieve restrictions due to {ErrorMessage}. Retrying in {Delay}ms. Retry attempt {RetryCount}/{MaxRetries}",
                        ex.Message, delay, retryCount + 1, maxRetries);

                    await Task.Delay(delay);
                    delay *= 2;
                    retryCount++;
                }
            }
        }

        public async Task<bool> MarkEventCompletedAsync(Guid id)
        {
            int maxRetries = 5;
            int retryCount = 0;
            int delay = 500;

            while (true)
            {
                try
                {
                    using var context = await _dbContextFactory.CreateDbContextAsync();
                    var calendarEvent = await context.Events.FindAsync(id);
                    
                    if (calendarEvent == null)
                        return false;
                        
                    calendarEvent.IsCompleted = true;
                    await context.SaveChangesAsync();
                    return true;
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Cannot access a disposed object") 
                    || ex.Message.Contains("An attempt was made to use the model while it was being created"))
                {
                    if (retryCount >= maxRetries)
                    {
                        _logger?.LogError(ex, "Failed to mark event completed after {RetryCount} retries", retryCount);
                        return false;
                    }

                    _logger?.LogWarning("Failed to mark event completed due to {ErrorMessage}. Retrying in {Delay}ms. Retry attempt {RetryCount}/{MaxRetries}",
                        ex.Message, delay, retryCount + 1, maxRetries);

                    await Task.Delay(delay);
                    delay *= 2;
                    retryCount++;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error marking event {EventId} as completed", id);
                    return false;
                }
            }
        }

        public async Task<bool> UpdateEventAsync(CalendarEvent calendarEvent)
        {
            int maxRetries = 5;
            int retryCount = 0;
            int delay = 500;

            while (true)
            {
                try
                {
                    using var context = await _dbContextFactory.CreateDbContextAsync();
                    context.Events.Update(calendarEvent);
                    await context.SaveChangesAsync();
                    return true;
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Cannot access a disposed object") 
                    || ex.Message.Contains("An attempt was made to use the model while it was being created"))
                {
                    if (retryCount >= maxRetries)
                    {
                        _logger?.LogError(ex, "Failed to update event after {RetryCount} retries", retryCount);
                        return false;
                    }

                    _logger?.LogWarning("Failed to update event due to {ErrorMessage}. Retrying in {Delay}ms. Retry attempt {RetryCount}/{MaxRetries}",
                        ex.Message, delay, retryCount + 1, maxRetries);

                    await Task.Delay(delay);
                    delay *= 2;
                    retryCount++;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error updating event {EventId}", calendarEvent.Id);
                    return false;
                }
            }
        }

        public async Task<bool> DeleteEventAsync(Guid id)
        {
            int maxRetries = 5;
            int retryCount = 0;
            int delay = 500;

            while (true)
            {
                try
                {
                    using var dbContext = await _dbContextFactory.CreateDbContextAsync();
                    
                    var calendarEvent = await dbContext.Events.FindAsync(id);
                    if (calendarEvent != null)
                    {
                        dbContext.Events.Remove(calendarEvent);
                        await dbContext.SaveChangesAsync();
                        return true;
                    }
                    return false;
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Cannot access a disposed object") 
                    || ex.Message.Contains("An attempt was made to use the model while it was being created"))
                {
                    if (retryCount >= maxRetries)
                    {
                        _logger?.LogError(ex, "Failed to delete event after {RetryCount} retries", retryCount);
                        return false;
                    }

                    _logger?.LogWarning("Failed to delete event due to {ErrorMessage}. Retrying in {Delay}ms. Retry attempt {RetryCount}/{MaxRetries}",
                        ex.Message, delay, retryCount + 1, maxRetries);

                    await Task.Delay(delay);
                    delay *= 2;
                    retryCount++;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error deleting event {EventId}", id);
                    return false;
                }
            }
        }

        public async Task<CalendarEvent> CreateEventAsync(CalendarEvent calendarEvent)
        {
            int maxRetries = 5;
            int retryCount = 0;
            int delay = 500;

            while (true)
            {
                try
                {
                    using var dbContext = await _dbContextFactory.CreateDbContextAsync();
                    
                    dbContext.Events.Add(calendarEvent);
                    await dbContext.SaveChangesAsync();
                    return calendarEvent;
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Cannot access a disposed object") 
                    || ex.Message.Contains("An attempt was made to use the model while it was being created"))
                {
                    if (retryCount >= maxRetries)
                    {
                        _logger?.LogError(ex, "Failed to create event after {RetryCount} retries", retryCount);
                        throw;
                    }

                    _logger?.LogWarning("Failed to create event due to {ErrorMessage}. Retrying in {Delay}ms. Retry attempt {RetryCount}/{MaxRetries}",
                        ex.Message, delay, retryCount + 1, maxRetries);

                    await Task.Delay(delay);
                    delay *= 2;
                    retryCount++;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error creating event: {Message}", ex.Message);
                    throw;
                }
            }
        }

        private async Task<List<TEntity>> GetEntitiesAsync<TEntity>(Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder, string entityName, CancellationToken cancellationToken = default) where TEntity : class
        {
            int maxRetries = 5;
            int retryCount = 0;
            int delay = 500;

            while (true)
            {
                try
                {
                    using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                    var dbSet = context.Set<TEntity>();
                    var query = queryBuilder(dbSet);
                    return await query.ToListAsync(cancellationToken);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Cannot access a disposed object") 
                    || ex.Message.Contains("An attempt was made to use the model while it was being created"))
                {
                    if (retryCount >= maxRetries)
                    {
                        _logger?.LogError(ex, "Failed to retrieve {EntityName} after {RetryCount} retries", entityName, retryCount);
                        throw;
                    }

                    _logger?.LogWarning("Failed to retrieve {EntityName} due to {ErrorMessage}. Retrying in {Delay}ms. Retry attempt {RetryCount}/{MaxRetries}",
                        entityName, ex.Message, delay, retryCount + 1, maxRetries);

                    await Task.Delay(delay, cancellationToken);
                    delay *= 2;
                    retryCount++;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error retrieving {EntityName}", entityName);
                    throw;
                }
            }
        }
    }
} 