using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using HairCarePlus.Shared.Common.CQRS;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Client.Patient.Features.Calendar.Application.Queries;

/// <summary>
/// Query that returns active (today or upcoming) restriction events.
/// </summary>
public sealed record GetActiveRestrictionsQuery() : IQuery<IReadOnlyList<RestrictionInfo>>;

public sealed class GetActiveRestrictionsHandler : IQueryHandler<GetActiveRestrictionsQuery, IReadOnlyList<RestrictionInfo>>
{
    private readonly ICalendarService _service;
    private readonly ILogger<GetActiveRestrictionsHandler> _logger;

    public GetActiveRestrictionsHandler(ICalendarService service, ILogger<GetActiveRestrictionsHandler> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RestrictionInfo>> HandleAsync(GetActiveRestrictionsQuery query, CancellationToken cancellationToken = default)
    {
        var restrictions = await _service.GetActiveRestrictionsAsync() ?? new List<CalendarEvent>();
        var today = DateTime.Today;
        var list = new List<RestrictionInfo>();

        foreach (var r in restrictions)
        {
            if (!r.EndDate.HasValue || r.EndDate.Value.Date < today) continue;

            var remaining = (int)Math.Ceiling((r.EndDate.Value.Date - today).TotalDays);
            remaining = Math.Max(1, remaining);

            list.Add(new RestrictionInfo
            {
                OriginalType = r.EventType,
                Description = r.Title,
                EndDate = r.EndDate.Value,
                RemainingDays = remaining,
                IconGlyph = string.Empty // UI will fill later
            });
        }

        _logger.LogDebug("GetActiveRestrictionsHandler returned {Count} items", list.Count);
        return list;
    }
} 