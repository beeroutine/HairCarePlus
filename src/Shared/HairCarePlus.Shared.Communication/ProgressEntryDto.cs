using System;

namespace HairCarePlus.Shared.Communication;

public sealed class ProgressEntryDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public DateTime DateUtc { get; set; }
    public int CompletedTasks { get; set; }
    public int TotalTasks { get; set; }
} 