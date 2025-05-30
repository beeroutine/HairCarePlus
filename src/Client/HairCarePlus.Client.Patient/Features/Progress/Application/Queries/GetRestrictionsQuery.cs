using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Shared.Common.CQRS;
using HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;
using HairCarePlus.Client.Patient.Features.Progress.Services.Interfaces;

namespace HairCarePlus.Client.Patient.Features.Progress.Application.Queries;

/// <summary>
/// CQRS-запрос, возвращающий актуальные ограничения пациента (таймеры).
/// </summary>
public sealed record GetRestrictionsQuery : IQuery<IReadOnlyList<RestrictionTimer>>;

/// <summary>
/// Обработчик <see cref="GetRestrictionsQuery"/>.
/// Делегирует логику сервису <see cref="IRestrictionService"/>, что позволяет подменять источник данных (SQLite, REST, SignalR и т.д.).
/// </summary>
public sealed class GetRestrictionsHandler : IQueryHandler<GetRestrictionsQuery, IReadOnlyList<RestrictionTimer>>
{
    private readonly IRestrictionService _service;

    public GetRestrictionsHandler(IRestrictionService service)
        => _service = service;

    public Task<IReadOnlyList<RestrictionTimer>> HandleAsync(GetRestrictionsQuery query, CancellationToken cancellationToken = default)
        => _service.GetActiveRestrictionsAsync(cancellationToken);
} 