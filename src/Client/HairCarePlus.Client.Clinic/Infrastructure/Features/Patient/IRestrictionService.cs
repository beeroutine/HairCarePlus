using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Client.Clinic.Infrastructure.Features.Patient.Models;

namespace HairCarePlus.Client.Clinic.Infrastructure.Features.Patient;

public interface IRestrictionService
{
    Task<IReadOnlyList<RestrictionDto>> GetRestrictionsAsync(string patientId);
} 