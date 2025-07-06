using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Client.Clinic.Infrastructure.Features.Patient.Models;

namespace HairCarePlus.Client.Clinic.Infrastructure.Features.Patient;

public sealed class RestrictionService : IRestrictionService
{
    public Task<IReadOnlyList<RestrictionDto>> GetRestrictionsAsync(string patientId)
    {
        var list = new List<RestrictionDto>
        {
            new()
            {
                Icon = "pill",
                DaysRemaining = 5,
                Progress = 0.4
            },
            new()
            {
                Icon = "no_sun",
                DaysRemaining = 10,
                Progress = 0.1
            }
        };

        return Task.FromResult<IReadOnlyList<RestrictionDto>>(list);
    }
} 