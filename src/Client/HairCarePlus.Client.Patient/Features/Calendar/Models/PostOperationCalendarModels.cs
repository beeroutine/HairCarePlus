using System;
using System.Collections.Generic;

namespace HairCarePlus.Client.Patient.Features.Calendar.Models
{
    public enum EventType
    {
        Medication,
        PhotoUpload,
        Instruction,
        Milestone,
        PRP,
        Restriction,
        Warning,
        WashingInstruction,
        ProgressCheck
    }

    public enum RecoveryPhase
    {
        Initial,           // 0-3 дня
        EarlyRecovery,    // 4-10 дней
        Healing,          // 11-30 дней
        Growth,           // 1-3 месяца
        Development,      // 4-8 месяца
        Final             // 9-12 месяца
    }

    public class CalendarEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Description { get; set; }
        public int StartDay { get; set; }
        public int? EndDay { get; set; }
        public EventType Type { get; set; }
        public bool IsRepeating { get; set; }
        public int? RepeatIntervalDays { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    public class MedicationEvent : CalendarEvent
    {
        public string MedicationName { get; set; }
        public string Dosage { get; set; }
        public int TimesPerDay { get; set; }
        public string Instructions { get; set; }
        public bool IsOptional { get; set; }
    }

    public class Restriction : CalendarEvent
    {
        public string Reason { get; set; }
        public string RecommendedAlternative { get; set; }
        public bool IsCritical { get; set; }
    }

    public class PhotoUploadEvent : CalendarEvent
    {
        public string[] RequiredAreas { get; set; }
        public string InstructionVideo { get; set; }
        public string[] RequiredAngles { get; set; }
    }

    public class MilestoneEvent : CalendarEvent
    {
        public string Achievement { get; set; }
        public string[] UnlockedActivities { get; set; }
        public string[] RemovedRestrictions { get; set; }
    }

    public class WashingInstructionEvent : CalendarEvent
    {
        public List<WashingStep> Steps { get; set; }
        public string[] RequiredItems { get; set; }
        public string[] Warnings { get; set; }
        public string VideoUrl { get; set; }
    }

    public class WashingStep
    {
        public int Order { get; set; }
        public string Description { get; set; }
        public int DurationInMinutes { get; set; }
        public string[] Tips { get; set; }
    }

    public class ProgressCheckEvent : CalendarEvent
    {
        public RecoveryPhase Phase { get; set; }
        public string[] ExpectedChanges { get; set; }
        public string[] CheckPoints { get; set; }
        public Dictionary<string, string> NormalConditions { get; set; }
        public Dictionary<string, string> WarningSignals { get; set; }
    }

    public class InstructionEvent : CalendarEvent
    {
        public string[] Steps { get; set; }
        public string[] Tips { get; set; }
        public string[] Cautions { get; set; }
        public string VideoUrl { get; set; }
    }
} 