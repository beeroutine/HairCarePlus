using System;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Shared.Common.CQRS;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Messaging;
using HairCarePlus.Client.Patient.Features.Calendar.Messages;

namespace HairCarePlus.Client.Patient.Features.Calendar.Application.Commands
{
    public sealed record ToggleEventCompletionCommand(Guid EventId) : ICommand;

    public sealed class ToggleEventCompletionHandler : ICommandHandler<ToggleEventCompletionCommand>
    {
        private readonly ICalendarService _calendarService;
        private readonly ICalendarCacheService _cache;
        private readonly ILogger<ToggleEventCompletionHandler> _logger;
        private readonly IMessenger _messenger;

        public ToggleEventCompletionHandler(ICalendarService calendarService,
                                            ICalendarCacheService cache,
                                            IMessenger messenger,
                                            ILogger<ToggleEventCompletionHandler> logger)
        {
            _calendarService = calendarService;
            _cache = cache;
            _messenger = messenger;
            _logger = logger;
        }

        public async Task HandleAsync(ToggleEventCompletionCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                var success = await _calendarService.MarkEventAsCompletedAsync(command.EventId);
                if (!success)
                {
                    _logger.LogWarning("MarkEventAsCompletedAsync returned false for {EventId}", command.EventId);
                    return;
                }

                // Invalidate cache entries that may contain this event (brute-force through known dates) – for demo
                // Real implementation should map id→date.
                _cache.CleanupOldEntries();

                _messenger.Send(new EventUpdatedMessage(command.EventId));
                _logger.LogInformation("ToggleEventCompletion handled for {EventId}", command.EventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling completion for {EventId}", command.EventId);
                throw;
            }
        }
    }
} 