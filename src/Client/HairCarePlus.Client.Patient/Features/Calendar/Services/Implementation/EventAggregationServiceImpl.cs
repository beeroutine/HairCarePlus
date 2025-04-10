using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using HairCarePlus.Client.Patient.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services.Implementation
{
    public class EventAggregationServiceImpl : IEventAggregationService
    {
        private readonly AppDbContext _dbContext;

        public EventAggregationServiceImpl(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Dictionary<TimeOfDay, List<CalendarEvent>>> GroupEventsByTimeOfDayAsync(IEnumerable<CalendarEvent> events)
        {
            var result = new Dictionary<TimeOfDay, List<CalendarEvent>>();

            // Initialize all time slots
            foreach (TimeOfDay timeOfDay in Enum.GetValues(typeof(TimeOfDay)))
            {
                result[timeOfDay] = new List<CalendarEvent>();
            }

            if (events == null)
                return Task.FromResult(result);

            // Group events by time of day
            var grouped = events.GroupBy(e => e.TimeOfDay);
            foreach (var group in grouped)
            {
                result[group.Key] = group.ToList();
            }

            return Task.FromResult(result);
        }

        public Task<Dictionary<EventType, int>> GetEventCountsByTypeAsync(IEnumerable<CalendarEvent> events)
        {
            var result = new Dictionary<EventType, int>();

            // Initialize counts for all event types
            foreach (EventType type in Enum.GetValues(typeof(EventType)))
            {
                result[type] = 0;
            }

            if (events == null)
                return Task.FromResult(result);

            // Count events by type
            foreach (var evt in events)
            {
                result[evt.EventType]++;
            }

            return Task.FromResult(result);
        }

        public async Task<Dictionary<DateTime, Dictionary<EventType, int>>> GetEventCountsByDateAndTypeAsync(
            IEnumerable<CalendarEvent> events,
            DateTime startDate,
            DateTime endDate)
        {
            var result = new Dictionary<DateTime, Dictionary<EventType, int>>();

            // Initialize the dictionary for each date in the range
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                result[date] = new Dictionary<EventType, int>();
                foreach (EventType type in Enum.GetValues(typeof(EventType)))
                {
                    result[date][type] = 0;
                }
            }

            if (events == null)
                return result;

            // Expand multi-day events first
            var expandedEvents = await ExpandMultiDayEventsAsync(events);

            // Count events by date and type
            foreach (var evt in expandedEvents)
            {
                var eventDate = evt.StartDate.Date;
                if (result.ContainsKey(eventDate))
                {
                    result[eventDate][evt.EventType]++;
                }
            }

            return result;
        }

        public async Task<IEnumerable<CalendarEvent>> ExpandMultiDayEventsAsync(IEnumerable<CalendarEvent> events)
        {
            var expandedEvents = new List<CalendarEvent>();

            if (events == null)
                return expandedEvents;

            foreach (var evt in events)
            {
                if (!evt.IsMultiDay)
                {
                    expandedEvents.Add(evt);
                    continue;
                }

                var currentDate = evt.StartDate;
                var endDate = evt.EndDate ?? evt.StartDate;

                while (currentDate <= endDate)
                {
                    var expandedEvent = new CalendarEvent
                    {
                        Id = evt.Id,
                        Title = evt.Title,
                        Description = evt.Description,
                        StartDate = currentDate,
                        EndDate = currentDate.AddDays(1).AddSeconds(-1),
                        EventType = evt.EventType,
                        Priority = evt.Priority,
                        TimeOfDay = evt.TimeOfDay,
                        IsCompleted = evt.IsCompleted,
                        CreatedAt = evt.CreatedAt,
                        ModifiedAt = evt.ModifiedAt,
                        ReminderTime = evt.ReminderTime,
                        ExpirationDate = evt.ExpirationDate
                    };

                    expandedEvents.Add(expandedEvent);
                    currentDate = currentDate.AddDays(1);
                }
            }

            return expandedEvents;
        }

        public async Task<IEnumerable<CalendarEvent>> GetEventsForDateAsync(DateTime date)
        {
            return await _dbContext.Events
                .Where(e => e.StartDate.Date == date.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<CalendarEvent>> GetEventsForDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbContext.Events
                .Where(e => e.StartDate.Date >= startDate.Date && e.StartDate.Date <= endDate.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<CalendarEvent>> GetActiveRestrictionsAsync()
        {
            var now = DateTime.UtcNow;
            return await _dbContext.Events
                .Where(e => e.EventType == EventType.CriticalWarning &&
                           e.StartDate <= now &&
                           (!e.EndDate.HasValue || e.EndDate.Value >= now))
                .ToListAsync();
        }

        public async Task<CalendarEvent?> GetEventByIdAsync(int eventId)
        {
            return await _dbContext.Events.FindAsync(eventId);
        }

        public async Task<bool> MarkEventCompletedAsync(int eventId)
        {
            var calendarEvent = await GetEventByIdAsync(eventId);
            if (calendarEvent == null) return false;

            calendarEvent.IsCompleted = true;
            calendarEvent.ModifiedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateEventAsync(CalendarEvent calendarEvent)
        {
            calendarEvent.ModifiedAt = DateTime.UtcNow;
            _dbContext.Events.Update(calendarEvent);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteEventAsync(int eventId)
        {
            var calendarEvent = await GetEventByIdAsync(eventId);
            if (calendarEvent == null) return false;

            _dbContext.Events.Remove(calendarEvent);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<CalendarEvent> CreateEventAsync(CalendarEvent calendarEvent)
        {
            calendarEvent.CreatedAt = DateTime.UtcNow;
            calendarEvent.ModifiedAt = DateTime.UtcNow;
            _dbContext.Events.Add(calendarEvent);
            await _dbContext.SaveChangesAsync();
            return calendarEvent;
        }
    }
} 