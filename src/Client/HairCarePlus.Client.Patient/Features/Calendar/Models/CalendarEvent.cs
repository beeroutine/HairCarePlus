using System;

namespace HairCarePlus.Client.Patient.Features.Calendar.Models
{
    public enum EventType
    {
        MedicationTreatment,  // Лекарства и процедуры лечения
        MedicalVisit,         // Осмотры, визиты в клинику
        Photo,                // Фотоотчёты
        Video,                // Видео-инструкции
        GeneralRecommendation,// Общие рекомендации и заметки
        CriticalWarning       // Критические предупреждения
    }

    public enum TimeOfDay
    {
        Morning,
        Afternoon,
        Evening
    }

    public enum EventPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    public class CalendarEvent
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public bool IsCompleted { get; set; }
        public EventType EventType { get; set; }
        public EventPriority Priority { get; set; }
        public TimeOfDay TimeOfDay { get; set; }
        public TimeSpan ReminderTime { get; set; }
        public DateTime? ExpirationDate { get; set; }

        // Свойство для определения, является ли событие длительным
        public bool IsMultiDay => EndDate.HasValue && EndDate.Value > StartDate;

        // Длительность события в днях
        public int DurationInDays => EndDate.HasValue ? 
            (EndDate.Value - StartDate).Days + 1 : 
            (StartDate - Date).Days + 1;
            
        // Свойство для определения, нужно ли отображать время для события
        public bool HasTime => EventType == EventType.MedicationTreatment;

        public CalendarEvent()
        {
            var now = DateTime.Now;
            Id = Guid.NewGuid();
            Date = now;
            StartDate = now;
            CreatedAt = now;
            ModifiedAt = now;
            Priority = EventPriority.Normal;
            TimeOfDay = TimeOfDay.Morning;
            ReminderTime = TimeSpan.FromMinutes(30);
        }
    }
} 