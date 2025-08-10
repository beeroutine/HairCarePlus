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

        // Ephemeral policy: server must not return historical PhotoReports.
        // Always return an empty list to force clients to rely on transient DeliveryQueue packets only.
        return Array.Empty<PhotoReportDto>();
    }
} 