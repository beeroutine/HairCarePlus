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
        /// Shows a success notification
        /// </summary>
        /// <param name="message">The message to display</param>
        Task ShowSuccessAsync(string message);
        
        /// <summary>
        /// Shows an error notification
        /// </summary>
        /// <param name="message">The message to display</param>
        Task ShowErrorAsync(string message);
        
        /// <summary>
        /// Shows a warning notification
        /// </summary>
        /// <param name="message">The message to display</param>
        Task ShowWarningAsync(string message);
        
        /// <summary>
        /// Shows an info notification
        /// </summary>
        /// <param name="message">The message to display</param>
        Task ShowInfoAsync(string message);
        
        /// <summary>
        /// Schedules a local notification
        /// </summary>
        /// <param name="title">Title of the notification</param>
        /// <param name="message">Message body</param>
        /// <param name="scheduledTime">When to show the notification</param>
        /// <returns>Notification identifier</returns>
        Task ScheduleNotificationAsync(string title, string message, DateTime scheduledTime);
        
        /// <summary>
        /// Cancels a scheduled notification
        /// </summary>
        /// <param name="notificationId">The id of the notification to cancel</param>
        Task CancelNotificationAsync(string notificationId);
        
        /// <summary>
        /// Requests notification permission from the user
        /// </summary>
        /// <returns>True if permission is granted, false otherwise</returns>
        Task<bool> RequestNotificationPermissionAsync();
    }
} 