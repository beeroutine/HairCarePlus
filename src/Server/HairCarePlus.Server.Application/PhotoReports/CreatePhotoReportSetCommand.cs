using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Shared.Communication;
using HairCarePlus.Server.Infrastructure.Data.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Server.Application.PhotoReports;

public sealed record CreatePhotoReportSetCommand(PhotoReportSetDto Set) : IRequest<PhotoReportSetDto>;

public sealed class CreatePhotoReportSetCommandHandler : IRequestHandler<CreatePhotoReportSetCommand, PhotoReportSetDto>
{
    private readonly IDeliveryQueueRepository _deliveryQueue;
    private readonly ILogger<CreatePhotoReportSetCommandHandler> _logger;
    private readonly HairCarePlus.Shared.Communication.Events.IEventsClient _events;

    public CreatePhotoReportSetCommandHandler(IDeliveryQueueRepository deliveryQueue,
                                             ILogger<CreatePhotoReportSetCommandHandler> logger,
                                             HairCarePlus.Shared.Communication.Events.IEventsClient events)
    {
        _deliveryQueue = deliveryQueue;
        _logger = logger;
        _events = events;
    }

    public async Task<PhotoReportSetDto> Handle(CreatePhotoReportSetCommand request, CancellationToken cancellationToken)
    {
        var set = request.Set;
        if (set == null) throw new ArgumentNullException(nameof(request.Set));
        if (set.Items == null || set.Items.Count != 3)
            throw new InvalidOperationException("PhotoReportSet must contain exactly three items");

        if (set.Items.Any(i => string.IsNullOrWhiteSpace(i.ImageUrl) || !i.ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Each PhotoReportSet item must have a valid HTTP ImageUrl");

        _logger.LogInformation("[PhotoReports] Enqueueing PhotoReportSet {SetId} for patient {PatientId}", set.Id, set.PatientId);

        var packet = new HairCarePlus.Server.Domain.Entities.DeliveryQueue
        {
            EntityType = nameof(PhotoReportSetDto),
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(set),
            PatientId = set.PatientId,
            ReceiversMask = 1, // Clinic
            DeliveredMask = 0,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(14)
        };

        await _deliveryQueue.AddRangeAsync(new[] { packet });
        await _events.PhotoReportSetAdded(set.PatientId.ToString(), set);

        return set;
    }
}


