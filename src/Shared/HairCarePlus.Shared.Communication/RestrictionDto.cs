using System;

namespace HairCarePlus.Shared.Communication;

public sealed class RestrictionDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public RestrictionType Type { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public bool IsActive { get; set; }
}

public enum RestrictionType
{
    Food,
    Alcohol,
    Sport,
    Sauna
} 