using System;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Shared.Common.CQRS;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Client.Patient.Features.Calendar.Application.Queries
{
    public sealed record GetEventByIdQuery(Guid EventId) : IQuery<CalendarEvent?>;

    public sealed class GetEventByIdHandler : IQueryHandler<GetEventByIdQuery, CalendarEvent?>
    {
        private readonly ICalendarService _calendarService;
        private readonly ILogger<GetEventByIdHandler> _logger;

        public GetEventByIdHandler(ICalendarService calendarService, ILogger<GetEventByIdHandler> logger)
        {
            _calendarService = calendarService;
            _logger = logger;
        }

        public async Task<CalendarEvent?> HandleAsync(GetEventByIdQuery query, CancellationToken cancellationToken = default)
        {
            try
            {
                var calendarEvent = await _calendarService.GetEventByIdAsync(query.EventId);
                return calendarEvent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event by ID {EventId}", query.EventId);
                return null;
            }
        }
    }
} 