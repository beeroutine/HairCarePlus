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

    public DateTime SurgeryDate { get; }

    public ProfileService()
    {
        // Try load stored value; if not present, use today and persist.
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
    }
} 