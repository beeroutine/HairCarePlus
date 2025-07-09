namespace HairCarePlus.Client.Clinic.Infrastructure.Features.Patient.Models;

public sealed class RestrictionDto
{
    public HairCarePlus.Shared.Domain.Restrictions.RestrictionIconType IconType { get; set; }
    public int DaysRemaining { get; set; }
    public double Progress { get; set; }
} 