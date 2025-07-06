using HairCarePlus.Server.Domain.ValueObjects;
using HairCarePlus.Server.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HairCarePlus.Server.Application.Progress;

// Query
public record GetProgressEntriesQuery(Guid PatientId) : IRequest<IReadOnlyList<ProgressEntry>>;

// Handler
public class GetProgressEntriesQueryHandler : IRequestHandler<GetProgressEntriesQuery, IReadOnlyList<ProgressEntry>>
{
    private readonly AppDbContext _db;

    public GetProgressEntriesQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ProgressEntry>> Handle(GetProgressEntriesQuery request, CancellationToken cancellationToken)
    {
        return await _db.ProgressEntries
            .Where(p => p.PatientId == request.PatientId)
            .OrderByDescending(p => p.DateUtc)
            .ToListAsync(cancellationToken);
    }
} 