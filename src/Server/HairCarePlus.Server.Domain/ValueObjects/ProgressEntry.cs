using HairCarePlus.Server.Domain.Entities;

namespace HairCarePlus.Server.Domain.ValueObjects;

public class ProgressEntry : BaseEntity
{
    public Guid PatientId { get; private set; }
    public DateTime DateUtc { get; private set; }
    public int CompletedTasks { get; private set; }
    public int TotalTasks { get; private set; }

    // for EF
    private ProgressEntry() { }

    public ProgressEntry(Guid id, Guid patientId, DateTime dateUtc, int completed, int total) : this(patientId, dateUtc, completed, total)
    {
        Id = id;
    }

    public ProgressEntry(Guid patientId, DateTime dateUtc, int completed, int total)
    {
        Id = Guid.NewGuid();
        PatientId = patientId;
        DateUtc = dateUtc;
        CompletedTasks = completed;
        TotalTasks = total;
        Update();
    }
} 