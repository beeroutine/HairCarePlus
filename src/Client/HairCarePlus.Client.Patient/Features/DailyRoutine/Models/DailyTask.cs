using System;

namespace HairCarePlus.Client.Patient.Features.DailyRoutine.Models
{
    public enum TaskType
    {
        Medication,
        Photo,
        Video,
        Appointment,
        General
    }

    public class DailyTask
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public TaskType TaskType { get; set; }
        public DateTime Time { get; set; }
        public bool IsCompleted { get; set; }
    }
} 