using System;

namespace HairCarePlus.Shared.Communication;

public sealed class CalendarTaskDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime DueDateUtc { get; set; }
    public bool IsDone { get; set; }
    public bool IsSkipped { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
} 