using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Models = HairCarePlus.Client.Clinic.Infrastructure.Features.Patient.Models;
using HairCarePlus.Client.Clinic.Infrastructure.Storage;
using HairCarePlus.Shared.Communication;
using HairCarePlus.Shared.Domain.Restrictions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Client.Clinic.Infrastructure.Features.Patient;

public sealed class RestrictionService : IRestrictionService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<RestrictionService> _logger;

    public RestrictionService(IDbContextFactory<AppDbContext> dbFactory, ILogger<RestrictionService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Models.RestrictionDto>> GetRestrictionsAsync(string patientId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entities = await db.Restrictions
            .Where(r => r.PatientId == patientId && r.IsActive)
            .ToListAsync();

        var list = entities
            .GroupBy(e => e.Type) // collapse by type, keep the one with nearest deadline
            .Select(g => g.OrderBy(e => (e.EndUtc.Date - DateTime.UtcNow.Date).Days).First())
            .Select(e => new Models.RestrictionDto
            {
                IconType = MapToIconType(e.Type),
                DaysRemaining = Math.Max(0, (e.EndUtc.Date - DateTime.UtcNow.Date).Days + 1),
                Progress = CalculateProgress(e.StartUtc, e.EndUtc)
            })
            .OrderBy(r => r.DaysRemaining)
            .ToList();

        // Verbose logging for diagnostics
        _logger.LogInformation("Fetched {Count} active restrictions for patient {PatientId}", list.Count, patientId);
        foreach (var r in list)
        {
            _logger.LogInformation("Restriction mapped: IconType={IconType}, DaysRemaining={Days}, Progress={Progress:p1}", r.IconType, r.DaysRemaining, r.Progress);
        }

        return list;
    }

    /// <summary>
    /// Returns normalized progress (0-1) using the **same integer, inclusive** algorithm
    /// as the Patient application so that visual rings are identical.
    /// </summary>
    private static double CalculateProgress(DateTime start, DateTime end)
    {
        // Treat both dates as whole days (ignore time-of-day) and include start & end dates.
        int totalDays = Math.Max(1, (end.Date - start.Date).Days + 1);

        int daysRemaining = Math.Max(0, (end.Date - DateTime.UtcNow.Date).Days + 1);
        int elapsedDays = Math.Clamp(totalDays - daysRemaining, 0, totalDays);

        return (double)elapsedDays / totalDays;
    }

    private static RestrictionIconType MapToIconType(int type) => (RestrictionIconType)type;
} 