using System;

namespace HairCarePlus.Client.Patient.Features.Calendar.Domain.Entities;

public class HairTransplantEvent
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsCompleted { get; set; }
    public EventType Type { get; set; }
    public EventPriority Priority { get; set; }
    public string Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
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