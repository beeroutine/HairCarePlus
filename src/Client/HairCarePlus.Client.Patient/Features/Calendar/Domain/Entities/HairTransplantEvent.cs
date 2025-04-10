using System;

namespace HairCarePlus.Client.Patient.Features.Calendar.Domain.Entities;

public class HairTransplantEvent
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public EventType Type { get; set; }
    public EventPriority Priority { get; set; }
    public bool IsCompleted { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }

    public bool IsMultiDay => EndDate.Date > StartDate.Date;
    public int DurationInDays => (EndDate.Date - StartDate.Date).Days + 1;
}

public enum EventType
{
    Medication,
    MedicalVisit,
    Photo,
    Video,
    Recommendation,
    Warning
}

public enum EventPriority
{
    Low,
    Normal,
    High,
    Critical
} 