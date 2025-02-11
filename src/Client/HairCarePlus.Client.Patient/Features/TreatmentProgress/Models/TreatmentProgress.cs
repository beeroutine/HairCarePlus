using System;
using System.Collections.Generic;

namespace HairCarePlus.Client.Patient.Features.TreatmentProgress.Models
{
    public class TreatmentProgress
    {
        public DateTime SurgeryDate { get; set; }
        public int DaysSinceSurgery { get; set; }
        public string CurrentPhase { get; set; }
        public int ProgressPercentage { get; set; }
        public string LastPhotoUrl { get; set; }
        public string FirstPhotoUrl { get; set; }
        public List<TreatmentMilestone> Milestones { get; set; } = new();
        public NextAction NextAction { get; set; }
    }

    public class TreatmentMilestone
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public bool IsCompleted { get; set; }
        public string PhotoUrl { get; set; }
    }

    public class NextAction
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public ActionType Type { get; set; }
        public string ActionData { get; set; } // URL, идентификатор или другие данные для действия
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