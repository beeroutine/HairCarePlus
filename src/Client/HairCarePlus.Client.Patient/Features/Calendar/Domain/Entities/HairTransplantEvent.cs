using System;

namespace HairCarePlus.Client.Patient.Features.Calendar.Domain.Entities;

public class HairTransplantEvent
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsNotified { get; set; }
    public EventType Type { get; set; }
    public EventPriority Priority { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}

public enum EventType
{
    Medication,
    Checkup,
    Washing,
    Exercise,
    Photo,
    Restriction,
    Warning,
    MedicalVisit,
    Other
}

public enum EventPriority
{
    Low,
    Normal,
    High,
    Critical
} 