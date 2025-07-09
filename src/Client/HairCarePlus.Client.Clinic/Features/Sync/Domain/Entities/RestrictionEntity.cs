using System;

namespace HairCarePlus.Client.Clinic.Features.Sync.Domain.Entities;

public class RestrictionEntity
{
    public string Id { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public int Type { get; set; } // maps Shared.Communication.RestrictionType enum value
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public bool IsActive { get; set; }
} 