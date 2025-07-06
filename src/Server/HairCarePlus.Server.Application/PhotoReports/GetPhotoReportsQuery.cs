using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Shared.Communication;
using MediatR;
using HairCarePlus.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Server.Application.PhotoReports;

public sealed record GetPhotoReportsQuery(Guid PatientId) : IRequest<IReadOnlyList<PhotoReportDto>>;

public sealed class GetPhotoReportsQueryHandler : IRequestHandler<GetPhotoReportsQuery, IReadOnlyList<PhotoReportDto>>
{
    private readonly AppDbContext _db;
    private readonly ILogger<GetPhotoReportsQueryHandler> _logger;

    public GetPhotoReportsQueryHandler(AppDbContext db, ILogger<GetPhotoReportsQueryHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PhotoReportDto>> Handle(GetPhotoReportsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[PhotoReports] Fetching reports for patient {PatientId}", request.PatientId);

        // EF Core + SQLite не умеет сортировать по DateTimeOffset → загружаем в память и сортируем там
        var entities = await _db.PhotoReports
                                .Include(r => r.Comments)
                                .Where(r => r.PatientId == request.PatientId)
                                .OrderByDescending(r => r.CaptureDate)
                                .ToListAsync(cancellationToken);

        var dtos = entities.Select(r => new PhotoReportDto
        {
            Id = r.Id,
            PatientId = r.PatientId,
            ImageUrl = r.ImageUrl,
            ThumbnailUrl = r.ThumbnailUrl,
            Date = r.CaptureDate,
            Notes = r.Notes,
            Type = (HairCarePlus.Shared.Communication.PhotoType)r.Type,
            Comments = r.Comments
                        .OrderByDescending(c => c.CreatedAtUtc)
                        .Select(c => new PhotoCommentDto
                        {
                            Id = c.Id,
                            PhotoReportId = c.PhotoReportId,
                            AuthorId = c.AuthorId,
                            Text = c.Text,
                            CreatedAtUtc = c.CreatedAtUtc
                        })
                        .ToList()
        }).ToList();

        return dtos;
    }
} 