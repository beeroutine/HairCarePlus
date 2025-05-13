using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;
using HairCarePlus.Client.Patient.Features.Progress.Services.Interfaces;

namespace HairCarePlus.Client.Patient.Features.Progress.Services.Implementation;

/// <summary>
/// MVP-заглушка, возвращает фиксированный набор ограничений.
/// Позже будет заменена на реальный сервис.
/// </summary>
public sealed class RestrictionServiceStub : IRestrictionService
{
    public Task<IReadOnlyList<RestrictionTimer>> GetActiveRestrictionsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<RestrictionTimer> list = new List<RestrictionTimer>
        {
            new() { Title = "Alcohol", DaysRemaining = 5 },
            new() { Title = "Gym", DaysRemaining = 12 },
            new() { Title = "Sauna", DaysRemaining = 15 }
        };
        return Task.FromResult(list);
    }
} 