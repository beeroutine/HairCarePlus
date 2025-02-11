using System;
using System.Collections.Generic;

namespace HairCarePlus.Client.Patient.Features.TreatmentProgress.Models
{
    public enum EventType
    {
        Medication,          // Прием лекарств
        PhotoReport,         // Фотоотчет
        Restriction,         // Ограничения
        Care,               // Уход (мытье головы и т.д.)
        Recommendation,     // Рекомендации
        PlasmaTherapy,      // Плазмотерапия
        Vitamins,           // Прием витаминов
        Milestone           // Важные этапы (шоковое выпадение, рост и т.д.)
    }

    public enum MedicationType
    {
        Prednol,            // Преднол/Прекорт
        Ciprasid,           // Ципрасид/Ципро/Ципронатин
        Apronax,            // Апронакс (обезболивающее)
        Vitamins            // Витамины
    }

    public enum RestrictionType
    {
        BendingDown,        // Наклоны
        SleepingPosition,   // Позиция сна
        Smoking,            // Курение
        Alcohol,            // Алкоголь
        Sports,             // Спорт
        Headwear,          // Головные уборы
        HairDyeing,        // Окрашивание волос
        Intimacy,          // Интимная жизнь
        Sweating,          // Потоотделение
        WaterActivities    // Бассейн, море, сауна и т.д.
    }

    public class TreatmentEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Date { get; set; }
        public EventType Type { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsRequired { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    public class MedicationEvent : TreatmentEvent
    {
        public MedicationType MedicationType { get; set; }
        public int DosageCount { get; set; }
        public string DosageUnit { get; set; } // таблетки, мл и т.д.
        public bool IsMorning { get; set; }
        public bool IsEvening { get; set; }
        public string WithFood { get; set; } // после еды, до еды и т.д.
    }

    public class RestrictionEvent : TreatmentEvent
    {
        public RestrictionType RestrictionType { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; }
        public string AlternativeSuggestion { get; set; }
    }

    public class PhotoReportEvent : TreatmentEvent
    {
        public List<string> RequiredAngles { get; set; } = new();
        public string LastPhotoUrl { get; set; }
        public Dictionary<string, string> Analysis { get; set; } = new();
        public bool NeedsAttention { get; set; }
    }

    public class CareEvent : TreatmentEvent
    {
        public string VideoGuideUrl { get; set; }
        public List<string> Steps { get; set; } = new();
        public string ProductToUse { get; set; }
        public int DurationMinutes { get; set; }
    }

    public class PlasmaTherapyEvent : TreatmentEvent
    {
        public int SessionNumber { get; set; }
        public decimal EstimatedCost { get; set; }
        public List<string> RecommendedClinics { get; set; } = new();
        public string PreparationInstructions { get; set; }
    }

    public class MilestoneEvent : TreatmentEvent
    {
        public string Phase { get; set; }
        public int DaysSinceSurgery { get; set; }
        public string ExpectedResult { get; set; }
        public List<string> CommonSymptoms { get; set; } = new();
        public List<string> WarningSignals { get; set; } = new();
    }
} 