using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Models = HairCarePlus.Client.Clinic.Infrastructure.Features.Patient.Models;
using HairCarePlus.Client.Clinic.Infrastructure.Storage;
using HairCarePlus.Shared.Communication;
using HairCarePlus.Shared.Domain.Restrictions;
using Microsoft.EntityFrameworkCore;

namespace HairCarePlus.Client.Clinic.Infrastructure.Features.Patient;

public sealed class RestrictionService : IRestrictionService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public RestrictionService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<Models.RestrictionDto>> GetRestrictionsAsync(string patientId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entities = await db.Restrictions
            .Where(r => r.PatientId == patientId && r.IsActive)
            .ToListAsync();

        var list = entities
            .Select(e => new Models.RestrictionDto
            {
                IconType = MapToIconType(e.Type),
                DaysRemaining = Math.Max(0, (e.EndUtc.Date - DateTime.UtcNow.Date).Days + 1),
                Progress = CalculateProgress(e.StartUtc, e.EndUtc)
            })
            // Preserve same ordering as patient (nearest deadline first)
            .OrderBy(r => r.DaysRemaining)
            .ToList();

        return list;
    }

    private static double CalculateProgress(DateTime start, DateTime end)
    {
        var total = (end - start).TotalDays;
        if (total <= 0) return 1.0;

        var elapsed = (DateTime.UtcNow - start).TotalDays;
        return Math.Clamp(elapsed / total, 0.0, 1.0);
    }

    private static RestrictionIconType MapToIconType(int type)
    {
        // сервер присылает HairCarePlus.Server.Domain.ValueObjects.RestrictionType
        return ((HairCarePlus.Shared.Communication.RestrictionType)type) switch
        {
            HairCarePlus.Shared.Communication.RestrictionType.Alcohol => RestrictionIconType.NoAlcohol,
            HairCarePlus.Shared.Communication.RestrictionType.Sport   => RestrictionIconType.NoSporting,
            HairCarePlus.Shared.Communication.RestrictionType.Sauna   => RestrictionIconType.NoSun,
            _                                                       => RestrictionIconType.NoSmoking
        };
    }
} 