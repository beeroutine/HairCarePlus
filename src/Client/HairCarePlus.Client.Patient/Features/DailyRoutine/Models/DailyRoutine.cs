using System;
using System.Collections.Generic;

namespace HairCarePlus.Client.Patient.Features.DailyRoutine.Models
{
    public class DailyRoutine
    {
        public DateTime Date { get; set; }
        public List<CareRoutine> MorningRoutines { get; set; } = new();
        public List<CareRoutine> EveningRoutines { get; set; } = new();
        public List<Medication> Medications { get; set; } = new();
        public List<Product> RequiredProducts { get; set; } = new();
    }

    public class CareRoutine
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public TimeSpan Duration { get; set; }
        public List<string> Steps { get; set; } = new();
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string VideoGuideUrl { get; set; }
        public Priority Priority { get; set; }
    }

    public class Medication
    {
        public string Name { get; set; }
        public string Dosage { get; set; }
        public TimeSpan Time { get; set; }
        public string Instructions { get; set; }
        public bool IsTaken { get; set; }
        public bool IsSkipped { get; set; }
        public DateTime? TakenAt { get; set; }
    }

    public class Product
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsAvailable { get; set; }
        public int RemainingDays { get; set; }
        public string PurchaseUrl { get; set; }
    }

    public enum Priority
    {
        Low,
        Medium,
        High,
        Critical
    }
} 