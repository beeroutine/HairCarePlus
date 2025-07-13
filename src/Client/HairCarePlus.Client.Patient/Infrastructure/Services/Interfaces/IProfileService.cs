using System;
namespace HairCarePlus.Client.Patient.Infrastructure.Services.Interfaces;

public interface IProfileService
{
    /// <summary>
    /// Date when surgery took place (or when app was first launched if user hasn\'t set exact date yet).
    /// Immutable for current device until user updates profile.
    /// </summary>
    DateTime SurgeryDate { get; }

    /// <summary>
    /// Globally unique identifier of current patient profile on this device.
    /// Until real authentication is introduced it is persisted locally and reused
    /// by sync & reporting features.
    /// </summary>
    Guid PatientId { get; }
} 