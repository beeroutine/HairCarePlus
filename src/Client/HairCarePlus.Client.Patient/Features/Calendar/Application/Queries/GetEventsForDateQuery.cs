using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using HairCarePlus.Shared.Common.CQRS;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Client.Patient.Features.Calendar.Application.Queries;

/// <summary>
/// Query that returns events for a particular calendar date.
/// </summary>
public sealed record GetEventsForDateQuery(DateTime Date) : IQuery<IEnumerable<CalendarEvent>>;

/// <summary>
/// Handler that loads events from cache when possible, otherwise queries the service and updates cache.
/// </summary>
public sealed class GetEventsForDateHandler : IQueryHandler<GetEventsForDateQuery, IEnumerable<CalendarEvent>>
{
    private readonly ICalendarCacheService _cache;
    private readonly ICalendarService _service;
    private readonly ILogger<GetEventsForDateHandler> _logger;
    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(10);

    public GetEventsForDateHandler(ICalendarCacheService cache, ICalendarService service, ILogger<GetEventsForDateHandler> logger)
    {
        _cache = cache;
        _service = service;
        _logger = logger;
    }

    public async Task<IEnumerable<CalendarEvent>> HandleAsync(GetEventsForDateQuery query, CancellationToken cancellationToken = default)
    {
        var dateKey = query.Date.Date;
        if (_cache.TryGet(dateKey, out var cachedEvents, out var lastUpd))
        {
            if ((DateTimeOffset.Now - lastUpd) <= _cacheTtl)
            {
                _logger.LogDebug("Cache hit for {Date}: {Count} events", dateKey, cachedEvents.Count);
                return cachedEvents;
            }
            _logger.LogDebug("Stale cache for {Date}: refreshing", dateKey);
        }

        _logger.LogDebug("Loading events from service for {Date}", dateKey);
        var events = (await _service.GetEventsForDateAsync(dateKey))?.ToList() ?? new List<CalendarEvent>();
        _cache.Set(dateKey, events);
        return events;
    }
} 