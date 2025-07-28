using System;
using HairCarePlus.Server.Domain.Entities;

namespace HairCarePlus.Server.Domain.ValueObjects
{
    public class Notification : BaseEntity
    {
        public string Title { get; private set; } = null!;
        public string Message { get; private set; } = null!;
        public NotificationType Type { get; private set; }
        public DateTime ScheduledTime { get; private set; }
        public bool IsRead { get; private set; }
        public bool IsSent { get; private set; }
        public DateTime? SentTime { get; private set; }
        public NotificationPriority Priority { get; private set; }

        private Notification() : base() { }

        public Notification(
            string title,
            string message,
            NotificationType type,
            DateTime scheduledTime,
            NotificationPriority priority)
        {
            Title = title;
            Message = message;
            Type = type;
            ScheduledTime = scheduledTime;
            Priority = priority;
            IsRead = false;
            IsSent = false;
        }

        public void MarkAsRead()
        {
            IsRead = true;
            Update();
        }

        public void MarkAsSent()
        {
            IsSent = true;
            SentTime = DateTime.UtcNow;
            Update();
        }

        public void UpdateScheduledTime(DateTime newScheduledTime)
        {
            ScheduledTime = newScheduledTime;
            Update();
        }
    }

    public enum NotificationType
    {
        MedicationReminder,
        PhotoReport,
        Treatment,
        Appointment,
        General,
        Alert
    }

    public enum NotificationPriority
    {
        Low,
        Normal,
        High,
        Urgent
    }
} 