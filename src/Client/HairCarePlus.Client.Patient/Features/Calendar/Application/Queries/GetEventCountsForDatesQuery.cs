using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using HairCarePlus.Shared.Common.CQRS;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Client.Patient.Features.Calendar.Application.Queries;

/// <summary>
/// Returns a nested dictionary with event counts by <see cref="EventType"/> for each requested date.
/// </summary>
/// <param name="Dates">List of calendar dates (Date component only is considered).</param>
public sealed record GetEventCountsForDatesQuery(IReadOnlyList<DateTime> Dates) : IQuery<Dictionary<DateTime, Dictionary<EventType, int>>>;

/// <summary>
/// Handler that leverages the cache whenever possible and falls back to <see cref="ICalendarService"/> for missing / stale data.
/// </summary>
public sealed class GetEventCountsForDatesHandler : IQueryHandler<GetEventCountsForDatesQuery, Dictionary<DateTime, Dictionary<EventType, int>>>
{
    private readonly ICalendarCacheService _cache;
    private readonly ICalendarService _service;
    private readonly ILogger<GetEventCountsForDatesHandler> _logger;
    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(10);

    public GetEventCountsForDatesHandler(ICalendarCacheService cache,
                                         ICalendarService service,
                                         ILogger<GetEventCountsForDatesHandler> logger)
    {
        _cache = cache;
        _service = service;
        _logger = logger;
    }

    public async Task<Dictionary<DateTime, Dictionary<EventType, int>>> HandleAsync(GetEventCountsForDatesQuery query, CancellationToken cancellationToken = default)
    {
        // Initialize result dictionary with zeroes for all requested dates
        var result = new Dictionary<DateTime, Dictionary<EventType, int>>();
        foreach (var date in query.Dates)
        {
            result[date.Date] = Enum.GetValues<EventType>()
                                    .ToDictionary(et => et, _ => 0);
        }

        foreach (var date in query.Dates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = date.Date;
            IEnumerable<CalendarEvent> events;

            if (_cache.TryGet(key, out var cachedEvents, out var lastUpd) &&
                (DateTimeOffset.Now - lastUpd) <= _cacheTtl)
            {
                _logger.LogDebug("Cache hit for {Date}", key);
                events = cachedEvents;
            }
            else
            {
                _logger.LogDebug("Loading events from service for {Date}", key);
                events = await _service.GetEventsForDateAsync(key) ?? Enumerable.Empty<CalendarEvent>();
                _cache.Set(key, events);
            }

            // Aggregate counts, considering multi-day events
            foreach (var evt in events)
            {
                if (evt.IsMultiDay && evt.EndDate.HasValue)
                {
                    for (var d = evt.Date.Date; d <= evt.EndDate.Value.Date; d = d.AddDays(1))
                    {
                        if (!result.ContainsKey(d)) continue; // out of the requested range
                        result[d][evt.EventType]++;
                    }
                }
                else
                {
                    if (result.ContainsKey(key))
                        result[key][evt.EventType]++;
                }
            }
        }

        return result;
    }
} 