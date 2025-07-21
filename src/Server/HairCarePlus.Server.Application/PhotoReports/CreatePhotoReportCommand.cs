using System;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Server.Domain.ValueObjects;
using HairCarePlus.Shared.Communication;
using MediatR;
using HairCarePlus.Server.Infrastructure.Data;
using HairCarePlus.Server.Infrastructure.Data.Repositories;
using HairCarePlus.Shared.Communication.Events;
using Microsoft.Extensions.Logging;
using DomainPhotoType = HairCarePlus.Server.Domain.ValueObjects.PhotoType;
using System.IO;

namespace HairCarePlus.Server.Application.PhotoReports;

public sealed record CreatePhotoReportCommand(Guid PatientId, string ImageUrl, DateTime CaptureDate) : IRequest<PhotoReportDto>;

public sealed class CreatePhotoReportCommandHandler : IRequestHandler<CreatePhotoReportCommand, PhotoReportDto>
{
    private readonly AppDbContext _db;
    private readonly IEventsClient _events;
    private readonly IDeliveryQueueRepository _deliveryQueue;
    private readonly ILogger<CreatePhotoReportCommandHandler> _logger;

    public CreatePhotoReportCommandHandler(AppDbContext db, IEventsClient events, IDeliveryQueueRepository deliveryQueue, ILogger<CreatePhotoReportCommandHandler> logger)
    {
        _db = db;
        _events = events;
        _deliveryQueue = deliveryQueue;
        _logger = logger;
    }

    public async Task<PhotoReportDto> Handle(CreatePhotoReportCommand request, CancellationToken cancellationToken)
    {
        // Validate that the referenced file has actually been uploaded – this prevents broken ImageUrl links
        // later causing 404 on clients.

        try
        {
            var uploadsDir = Path.Combine(AppContext.BaseDirectory, "uploads");
            var fileName = Path.GetFileName(new Uri(request.ImageUrl, UriKind.Absolute).LocalPath);
            var physicalPath = Path.Combine(uploadsDir, fileName);

            if (!File.Exists(physicalPath))
            {
                _logger.LogWarning("[PhotoReports] Image file '{File}' not found. Aborting report creation for patient {PatientId}", physicalPath, request.PatientId);
                throw new FileNotFoundException("Image file for photo-report was not found on server", physicalPath);
            }
        }
        catch (Exception ex) when (ex is UriFormatException or FileNotFoundException)
        {
            // Re-throw as validation exception so upper layers can return 400 BadRequest.
            throw new InvalidOperationException("Invalid ImageUrl supplied for CreatePhotoReport", ex);
        }

        _logger.LogInformation("[PhotoReports] Creating report for patient {PatientId}", request.PatientId);

        // Generate deterministic GUID so client side can reference this report
        var reportId = Guid.NewGuid();

        var dto = new PhotoReportDto
        {
            Id = reportId,
            PatientId = request.PatientId,
            ImageUrl = request.ImageUrl,
            ThumbnailUrl = string.Empty,
            Date = request.CaptureDate,
            Notes = string.Empty,
            Type = HairCarePlus.Shared.Communication.PhotoType.FrontView,
            Comments = new()
        };

        _logger.LogInformation("[PhotoReports] Enqueueing transient photo-report {ReportId} for patient {PatientId}", dto.Id, dto.PatientId);

        // enqueue for clinic side (receiver mask 1)
        var packet = new HairCarePlus.Server.Domain.Entities.DeliveryQueue
        {
            EntityType = "PhotoReport",
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(dto),
            PatientId = dto.PatientId,
            ReceiversMask = 1, // Clinic
            DeliveredMask = 0,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(14)
        };
        await _deliveryQueue.AddRangeAsync(new[] { packet });

        await _events.PhotoReportAdded(request.PatientId.ToString(), dto);

        return dto;
    }
}