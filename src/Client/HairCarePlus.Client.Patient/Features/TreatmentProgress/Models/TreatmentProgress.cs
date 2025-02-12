using System;
using System.Collections.Generic;

namespace HairCarePlus.Client.Patient.Features.TreatmentProgress.Models
{
    public class TreatmentProgress
    {
        public DateTime SurgeryDate { get; set; }
        public int DaysSinceSurgery { get; set; }
        public required string CurrentPhase { get; set; }
        public int ProgressPercentage { get; set; }
        public required string LastPhotoUrl { get; set; }
        public required string FirstPhotoUrl { get; set; }
        public List<TreatmentMilestone> Milestones { get; set; } = new();
        public required NextAction NextAction { get; set; }
    }

    public class TreatmentMilestone
    {
        public required string Title { get; set; }
        public required string Description { get; set; }
        public DateTime Date { get; set; }
        public bool IsCompleted { get; set; }
        public required string PhotoUrl { get; set; }
    }

    public class NextAction
    {
        public required string Title { get; set; }
        public required string Description { get; set; }
        public DateTime DueDate { get; set; }
        public ActionType Type { get; set; }
        public required string ActionData { get; set; }
    }

    public enum ActionType
    {
        TakePhoto,
        Medication,
        Procedure,
        DoctorAppointment,
        Consultation
    }
} 