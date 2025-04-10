using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Models;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces
{
    public interface ICalendarService
    {
        Task<List<CalendarEvent>> GetEventsForDateAsync(DateTime date);
        Task<List<CalendarEvent>> GetEventsForMonthAsync(int year, int month);
        Task<List<CalendarEvent>> GetEventsForDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<CalendarEvent>> GetActiveRestrictionsAsync();
        Task<List<CalendarEvent>> GetPendingNotificationEventsAsync();
        Task<List<CalendarEvent>> GetOverdueEventsAsync();
        Task<CalendarEvent> GetEventByIdAsync(int eventId);
        Task<bool> MarkEventAsCompletedAsync(int eventId);
        Task<bool> UpdateEventAsync(CalendarEvent calendarEvent);
        Task<bool> DeleteEventAsync(int eventId);
        Task<CalendarEvent> CreateEventAsync(CalendarEvent calendarEvent);
        Task<bool> SynchronizeEventsAsync();
    }
} 