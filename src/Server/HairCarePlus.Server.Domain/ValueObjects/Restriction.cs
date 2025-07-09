using HairCarePlus.Server.Domain.Entities;

namespace HairCarePlus.Server.Domain.ValueObjects;

public class Restriction : BaseEntity
{
    public Guid PatientId { get; private set; }
    public RestrictionType Type { get; private set; }
    public DateTime StartUtc { get; private set; }
    public DateTime EndUtc { get; private set; }
    public bool IsActive { get; private set; }

    private Restriction() { }

    public Restriction(Guid id, Guid patientId, RestrictionType type, DateTime start, DateTime end, bool isActive)
    {
        Id = id;
        PatientId = patientId;
        Type = type;
        StartUtc = start;
        EndUtc = end;
        IsActive = isActive;
    }
}

public enum RestrictionType
{
    NoSmoking = 0,
    NoAlcohol = 1,
    NoSex = 2,
    NoHairCutting = 3,
    NoHatWearing = 4,
    NoStyling = 5,
    NoLaying = 6,
    NoSun = 7,
    NoSweating = 8,
    NoSwimming = 9,
    NoSporting = 10,
    NoTilting = 11
} 