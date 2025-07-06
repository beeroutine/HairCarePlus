using System;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Server.Domain.ValueObjects;
using HairCarePlus.Shared.Communication;
using MediatR;
using HairCarePlus.Server.Infrastructure.Data;
using HairCarePlus.Shared.Communication.Events;
using Microsoft.Extensions.Logging;
using DomainPhotoType = HairCarePlus.Server.Domain.ValueObjects.PhotoType;

namespace HairCarePlus.Server.Application.PhotoReports;

public sealed record CreatePhotoReportCommand(Guid PatientId, string ImageUrl, DateTime CaptureDate) : IRequest<PhotoReportDto>;

public sealed class CreatePhotoReportCommandHandler : IRequestHandler<CreatePhotoReportCommand, PhotoReportDto>
{
    private readonly AppDbContext _db;
    private readonly IEventsClient _events;
    private readonly ILogger<CreatePhotoReportCommandHandler> _logger;

    public CreatePhotoReportCommandHandler(AppDbContext db, IEventsClient events, ILogger<CreatePhotoReportCommandHandler> logger)
    {
        _db = db;
        _events = events;
        _logger = logger;
    }

    public async Task<PhotoReportDto> Handle(CreatePhotoReportCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[PhotoReports] Creating report for patient {PatientId}", request.PatientId);

        var entity = new PhotoReport(
            request.ImageUrl,
            thumbnailUrl: string.Empty,
            patientId: request.PatientId,
            captureDate: request.CaptureDate,
            notes: string.Empty,
            type: DomainPhotoType.FrontView);

        _db.PhotoReports.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[PhotoReports] Created report {ReportId} for patient {PatientId}", entity.Id, request.PatientId);

        var dto = new PhotoReportDto
        {
            Id = entity.Id,
            PatientId = entity.PatientId,
            ImageUrl = entity.ImageUrl,
            ThumbnailUrl = entity.ThumbnailUrl,
            Date = entity.CaptureDate,
            Notes = entity.Notes,
            Type = (HairCarePlus.Shared.Communication.PhotoType)entity.Type,
            Comments = new()
        };

        // notify
        await _events.PhotoReportAdded(request.PatientId.ToString(), dto);

        return dto;
    }
}