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
        public required string Title { get; set; }
        public required string Description { get; set; }
        public TimeSpan Duration { get; set; }
        public List<string> Steps { get; set; } = new();
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public required string VideoGuideUrl { get; set; }
        public Priority Priority { get; set; }

        public CareRoutine()
        {
            Title = string.Empty;
            Description = string.Empty;
            VideoGuideUrl = string.Empty;
        }
    }

    public class Medication
    {
        public required string Name { get; set; }
        public required string Dosage { get; set; }
        public TimeSpan Time { get; set; }
        public required string Instructions { get; set; }
        public bool IsTaken { get; set; }
        public bool IsSkipped { get; set; }
        public DateTime? TakenAt { get; set; }
    }

    public class Product
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public bool IsAvailable { get; set; }
        public int RemainingDays { get; set; }
        public required string PurchaseUrl { get; set; }

        public Product()
        {
            Name = string.Empty;
            Description = string.Empty;
            PurchaseUrl = string.Empty;
        }
    }

    public enum Priority
    {
        Low,
        Medium,
        High,
        Critical
    }
} 