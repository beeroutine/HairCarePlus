using System;
using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Features.Notifications.Services.Interfaces
{
    /// <summary>
    /// Interface for notification service
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Schedules a local notification
        /// </summary>
        /// <param name="title">Title of the notification</param>
        /// <param name="message">Message body</param>
        /// <param name="scheduledTime">When to show the notification</param>
        /// <param name="data">Additional data to include with the notification</param>
        /// <returns>Notification identifier</returns>
        Task<string> ScheduleNotificationAsync(string title, string message, DateTime scheduledTime, string data = null);
        
        /// <summary>
        /// Cancels a scheduled notification
        /// </summary>
        /// <param name="notificationId">The id of the notification to cancel</param>
        Task CancelNotificationAsync(string notificationId);
        
        /// <summary>
        /// Cancels all scheduled notifications
        /// </summary>
        Task CancelAllNotificationsAsync();
    }
} 