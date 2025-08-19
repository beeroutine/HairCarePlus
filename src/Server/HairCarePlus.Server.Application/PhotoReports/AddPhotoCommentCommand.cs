using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Server.Domain.ValueObjects;
using HairCarePlus.Shared.Communication;
using HairCarePlus.Shared.Communication.Events;
using MediatR;
using HairCarePlus.Server.Infrastructure.Data;
using HairCarePlus.Server.Infrastructure.Data.Repositories;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Server.Application.PhotoReports;

public sealed record AddPhotoCommentCommand(Guid PatientId, Guid PhotoReportId, Guid AuthorId, string Text) : IRequest<PhotoCommentDto>;

public sealed class AddPhotoCommentCommandHandler : IRequestHandler<AddPhotoCommentCommand, PhotoCommentDto>
{
    private readonly AppDbContext _db;
    private readonly IEventsClient _events;
    private readonly IDeliveryQueueRepository _deliveryQueue;
    private readonly ILogger<AddPhotoCommentCommandHandler> _logger;

    public AddPhotoCommentCommandHandler(AppDbContext db, IEventsClient events, IDeliveryQueueRepository deliveryQueue, ILogger<AddPhotoCommentCommandHandler> logger)
    {
        _db = db;
        _events = events;
        _deliveryQueue = deliveryQueue;
        _logger = logger;
    }

    public async Task<PhotoCommentDto> Handle(AddPhotoCommentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[PhotoReports] Adding comment for report {ReportId} by author {AuthorId}", request.PhotoReportId, request.AuthorId);

        // Ephemeral behavior: do not require parent PhotoReport to exist on the server.
        // Build DTO directly and enqueue + broadcast.
        var dto = new PhotoCommentDto
        {
            Id = Guid.NewGuid(),
            PhotoReportId = request.PhotoReportId,
            AuthorId = request.AuthorId,
            Text = request.Text,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        // enqueue for clinic side
        var packet = new HairCarePlus.Server.Domain.Entities.DeliveryQueue
        {
            EntityType = "PhotoComment",
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(dto),
            PatientId = request.PatientId,
            ReceiversMask = 1,
            DeliveredMask = 0,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(14)
        };
        await _deliveryQueue.AddRangeAsync(new[] { packet });

        await _events.PhotoCommentAdded(request.PatientId.ToString(), request.PhotoReportId.ToString(), dto);

        _logger.LogInformation("[PhotoReports] Enqueued comment {CommentId} to report {ReportId}", dto.Id, request.PhotoReportId);

        return dto;
    }
}