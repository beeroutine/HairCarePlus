using System;
using Microsoft.Maui.Storage;
using HairCarePlus.Client.Patient.Infrastructure.Services.Interfaces;

namespace HairCarePlus.Client.Patient.Infrastructure.Services;

/// <summary>
/// Simple implementation that persists <see cref="IProfileService.SurgeryDate"/> in <see cref="Preferences"/>.
/// On first run sets the date to <see cref="DateTime.Today"/>.
/// Later this can be replaced by backend-synced profile but the API surface remains.
/// </summary>
public sealed class ProfileService : IProfileService
{
    private const string SurgeryDateKey = "surgery_date";
    private const string PatientIdKey = "patient_id";

    /// <summary>
    /// Default hard-coded patient identifier used across the app until user authentication
    /// is implemented and a backend-generated id is provided.
    /// Must stay in sync with SyncService._patientId constant.
    /// </summary>
    private static readonly Guid DefaultPatientId = Guid.Parse("35883846-63ee-4cf8-b930-25e61ec1f540");

    public DateTime SurgeryDate { get; }

    public Guid PatientId { get; }

    public ProfileService()
    {
        // Load persisted surgery date
        if (Preferences.ContainsKey(SurgeryDateKey))
        {
            var ticks = Preferences.Get(SurgeryDateKey, DateTime.Today.Ticks);
            SurgeryDate = new DateTime(ticks).Date;
        }
        else
        {
            SurgeryDate = DateTime.Today;
            Preferences.Set(SurgeryDateKey, SurgeryDate.Ticks);
        }

        // Load persisted patient id or fallback to default constant
        if (Preferences.ContainsKey(PatientIdKey))
        {
            var raw = Preferences.Get(PatientIdKey, DefaultPatientId.ToString());
            if (!Guid.TryParse(raw, out var parsed))
                parsed = DefaultPatientId;
            PatientId = parsed;
        }
        else
        {
            PatientId = DefaultPatientId;
            Preferences.Set(PatientIdKey, PatientId.ToString());
        }
    }
} 