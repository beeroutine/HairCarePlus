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

        var report = await _db.PhotoReports.FindAsync(new object[] { request.PhotoReportId }, cancellationToken);
        if (report is null)
            throw new InvalidOperationException("PhotoReport not found");

        var comment = new PhotoComment(request.PhotoReportId, request.AuthorId, request.Text);
        _db.PhotoComments.Add(comment);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = new PhotoCommentDto
        {
            Id = comment.Id,
            PhotoReportId = comment.PhotoReportId,
            AuthorId = comment.AuthorId,
            Text = comment.Text,
            CreatedAtUtc = comment.CreatedAtUtc
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

        _logger.LogInformation("[PhotoReports] Added comment {CommentId} to report {ReportId}", comment.Id, request.PhotoReportId);

        return dto;
    }
}