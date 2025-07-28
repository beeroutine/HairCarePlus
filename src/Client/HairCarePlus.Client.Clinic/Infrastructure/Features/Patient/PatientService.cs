using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Client.Clinic.Infrastructure.Features.Patient.Models;

namespace HairCarePlus.Client.Clinic.Infrastructure.Features.Patient;

public sealed class PatientService : IPatientService
{
    public Task<IReadOnlyList<PatientSummaryDto>> GetPatientsAsync()
    {
        // TODO: Replace with real API call
        var sample = new List<PatientSummaryDto>
        {
            new()
            {
                Id = "35883846-63ee-4cf8-b930-25e61ec1f540",
                Name = "Анна Петрова",
                DayProgress = 0.75,
                PhotoMissing = false,
                UnreadCount = 2,
                AvatarUrl = "https://placehold.co/100x100/png"
            }
        };
        return Task.FromResult<IReadOnlyList<PatientSummaryDto>>(sample);
    }
} 