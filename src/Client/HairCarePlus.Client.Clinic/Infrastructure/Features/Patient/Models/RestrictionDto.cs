namespace HairCarePlus.Client.Clinic.Infrastructure.Features.Patient.Models;

public sealed class RestrictionDto
{
    public string Icon { get; set; } = string.Empty;
    public int DaysRemaining { get; set; }
    public double Progress { get; set; }
} 