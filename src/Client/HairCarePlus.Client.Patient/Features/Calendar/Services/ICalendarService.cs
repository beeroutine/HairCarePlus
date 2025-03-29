using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Models;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services
{
    /// <summary>
    /// Interface for the Calendar Service
    /// </summary>
    public interface ICalendarService
    {
        /// <summary>
        /// Gets events for a specific date
        /// </summary>
        Task<IEnumerable<CalendarEvent>> GetEventsForDateAsync(DateTime date);
        
        /// <summary>
        /// Gets events for a specific month
        /// </summary>
        Task<IEnumerable<CalendarEvent>> GetEventsForMonthAsync(int year, int month);
        
        /// <summary>
        /// Gets events for a date range
        /// </summary>
        Task<IEnumerable<CalendarEvent>> GetEventsForDateRangeAsync(DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Marks an event as completed
        /// </summary>
        Task MarkEventAsCompletedAsync(int eventId, bool isCompleted);
        
        /// <summary>
        /// Gets active restrictions that haven't expired yet
        /// </summary>
        Task<IEnumerable<CalendarEvent>> GetActiveRestrictionsAsync();
        
        /// <summary>
        /// Gets events requiring notification
        /// </summary>
        Task<IEnumerable<CalendarEvent>> GetPendingNotificationEventsAsync();
        
        /// <summary>
        /// Gets events that are overdue (date in the past and not completed)
        /// </summary>
        Task<IEnumerable<CalendarEvent>> GetOverdueEventsAsync();
    }
} 