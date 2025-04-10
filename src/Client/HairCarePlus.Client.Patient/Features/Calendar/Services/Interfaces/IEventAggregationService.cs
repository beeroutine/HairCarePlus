using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Models;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;

/// <summary>
/// Service for aggregating and grouping calendar events
/// </summary>
public interface IEventAggregationService
{
    /// <summary>
    /// Groups events by time of day (Morning, Afternoon, Evening)
    /// </summary>
    Task<Dictionary<TimeOfDay, List<CalendarEvent>>> GroupEventsByTimeOfDayAsync(IEnumerable<CalendarEvent> events);

    /// <summary>
    /// Gets event counts grouped by event type for a collection of events
    /// </summary>
    Task<Dictionary<EventType, int>> GetEventCountsByTypeAsync(IEnumerable<CalendarEvent> events);

    /// <summary>
    /// Gets event counts for each date in a range, grouped by event type
    /// </summary>
    Task<Dictionary<DateTime, Dictionary<EventType, int>>> GetEventCountsByDateAndTypeAsync(
        IEnumerable<CalendarEvent> events,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Expands multi-day events into individual day events
    /// </summary>
    Task<IEnumerable<CalendarEvent>> ExpandMultiDayEventsAsync(IEnumerable<CalendarEvent> events);

    Task<IEnumerable<CalendarEvent>> GetEventsForDateAsync(DateTime date);
    Task<IEnumerable<CalendarEvent>> GetEventsForDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<CalendarEvent>> GetActiveRestrictionsAsync();
    Task<CalendarEvent?> GetEventByIdAsync(int eventId);
    Task<bool> MarkEventCompletedAsync(int eventId);
    Task<bool> UpdateEventAsync(CalendarEvent calendarEvent);
    Task<bool> DeleteEventAsync(int eventId);
    Task<CalendarEvent> CreateEventAsync(CalendarEvent calendarEvent);
} 