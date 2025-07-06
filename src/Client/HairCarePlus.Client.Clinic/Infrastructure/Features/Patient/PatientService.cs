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
                Id = "8f8c7e0b-1234-4e78-a8cc-ff0011223344",
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