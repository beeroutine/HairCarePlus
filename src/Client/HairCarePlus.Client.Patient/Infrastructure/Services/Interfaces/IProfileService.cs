namespace HairCarePlus.Client.Patient.Infrastructure.Services.Interfaces;

public interface IProfileService
{
    /// <summary>
    /// Date when surgery took place (or when app was first launched if user hasn\'t set exact date yet).
    /// Immutable for current device until user updates profile.
    /// </summary>
    DateTime SurgeryDate { get; }
} 