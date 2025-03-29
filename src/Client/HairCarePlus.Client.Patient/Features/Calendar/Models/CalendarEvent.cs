using System;

namespace HairCarePlus.Client.Patient.Features.Calendar.Models
{
    public enum EventType
    {
        Medication,
        Photo,
        Restriction,
        Instruction
    }

    public enum TimeOfDay
    {
        Morning,
        Afternoon,
        Evening
    }

    public class CalendarEvent
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public DateTime? EndDate { get; set; } // Конечная дата для длительных событий
        public string Title { get; set; }
        public string Description { get; set; }
        public EventType EventType { get; set; }
        public TimeOfDay TimeOfDay { get; set; }
        public bool IsCompleted { get; set; }
        public TimeSpan? ReminderTime { get; set; }
        public DateTime? ExpirationDate { get; set; } // For restrictions

        // Свойство для определения, является ли событие длительным
        public bool IsMultiDay => EndDate.HasValue && EndDate.Value > Date;

        // Длительность события в днях
        public int DurationInDays => EndDate.HasValue 
            ? (EndDate.Value - Date).Days + 1 
            : 1;
    }
} 