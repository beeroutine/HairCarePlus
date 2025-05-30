using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Shared.Common.CQRS;
using HairCarePlus.Client.Patient.Features.PhotoCapture.Domain.Entities;

namespace HairCarePlus.Client.Patient.Features.PhotoCapture.Application.Queries;

public sealed record GetCaptureTemplatesQuery() : IQuery<IReadOnlyList<CaptureTemplate>>;

public sealed class GetCaptureTemplatesHandler : IQueryHandler<GetCaptureTemplatesQuery, IReadOnlyList<CaptureTemplate>>
{
    public Task<IReadOnlyList<CaptureTemplate>> HandleAsync(GetCaptureTemplatesQuery query, CancellationToken cancellationToken = default)
    {
        // Пока используем жёстко заданные шаблоны. Позже можно загружать с сервера.
        IReadOnlyList<CaptureTemplate> templates = new List<CaptureTemplate>
        {
            new() { Id = "front", Name = "Фронт", OverlayAsset = "front_head.png", RecommendedDistanceMm = 300 },
            new() { Id = "top", Name = "Темя", OverlayAsset = "top_head.png", RecommendedDistanceMm = 350 },
            new() { Id = "back", Name = "Затылок", OverlayAsset = "back_head.png", RecommendedDistanceMm = 300 }
        };

        return Task.FromResult(templates);
    }
} 