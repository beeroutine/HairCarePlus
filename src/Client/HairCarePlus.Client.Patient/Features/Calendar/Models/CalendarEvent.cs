using System;

namespace HairCarePlus.Client.Patient.Features.Calendar.Models
{
    public enum EventType
    {
        MedicationTreatment,  // Лекарства и процедуры лечения (было Medication)
        MedicalVisit,         // Осмотры, визиты в клинику (новый тип)
        Photo,                // Фотоотчёты (без изменений)
        VideoInstruction,     // Видео-инструкции (было Instruction)
        GeneralRecommendation,// Общие рекомендации и заметки (новый тип)
        CriticalWarning       // Критические предупреждения (было Restriction)
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
        public EventPriority Priority { get; set; } = EventPriority.Normal;

        // Свойство для определения, является ли событие длительным
        public bool IsMultiDay => EndDate.HasValue && EndDate.Value > Date;

        // Длительность события в днях
        public int DurationInDays => EndDate.HasValue 
            ? (EndDate.Value - Date).Days + 1 
            : 1;
            
        // Свойство для определения, нужно ли отображать время для события
        // Показываем время только для событий с лекарствами, а для остальных - иконку
        public bool HasTime => EventType == EventType.MedicationTreatment;
    }
} 