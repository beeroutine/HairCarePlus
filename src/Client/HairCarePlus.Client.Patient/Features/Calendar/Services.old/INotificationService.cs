using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Models;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services
{
    public interface INotificationService
    {
        /// <summary>
        /// Schedules a notification for a calendar event
        /// </summary>
        Task ScheduleEventNotificationAsync(CalendarEvent calendarEvent);
        
        /// <summary>
        /// Cancels a scheduled notification
        /// </summary>
        Task CancelEventNotificationAsync(int eventId);
        
        /// <summary>
        /// Synchronizes all notifications based on current calendar events
        /// </summary>
        Task SynchronizeNotificationsAsync();
    }
} 