namespace HairCarePlus.Client.Clinic.Infrastructure.Features.Patient.Models;

public sealed class PatientSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double DayProgress { get; set; }
    public bool PhotoMissing { get; set; }
    public int UnreadCount { get; set; }
    public string? AvatarUrl { get; set; }
} 