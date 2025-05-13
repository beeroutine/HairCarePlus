namespace HairCarePlus.Client.Patient.Features.Progress.Services.Interfaces;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;

/// <summary>
/// Возвращает текущие ограничения и время до их снятия.
/// В дальнейшем будет реализовано через REST/SignalR обращение к серверу.
/// </summary>
public interface IRestrictionService
{
    Task<IReadOnlyList<RestrictionTimer>> GetActiveRestrictionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает список ограничений, актуальных на указанную дату после операции.
    /// </summary>
    Task<IReadOnlyList<RestrictionTimer>> GetRestrictionsForDateAsync(DateOnly date, CancellationToken cancellationToken = default);
} 