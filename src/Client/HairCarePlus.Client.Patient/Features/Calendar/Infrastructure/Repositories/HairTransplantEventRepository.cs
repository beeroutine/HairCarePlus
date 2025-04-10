using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HairCarePlus.Client.Patient.Features.Calendar.Domain.Entities;
using HairCarePlus.Client.Patient.Features.Calendar.Domain.Repositories;
using HairCarePlus.Client.Patient.Infrastructure.Storage;
using HairCarePlus.Client.Patient.Infrastructure.Storage.Repositories;

namespace HairCarePlus.Client.Patient.Features.Calendar.Infrastructure.Repositories;

public class HairTransplantEventRepository : BaseRepository<HairTransplantEvent>, IHairTransplantEventRepository
{
    public HairTransplantEventRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<HairTransplantEvent>> GetEventsForDateAsync(DateTime date)
    {
        return await DbSet
            .Where(e => e.StartDate.Date <= date.Date && e.EndDate.Date >= date.Date)
            .OrderBy(e => e.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<HairTransplantEvent>> GetEventsForRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await DbSet
            .Where(e => e.StartDate.Date <= endDate.Date && e.EndDate.Date >= startDate.Date)
            .OrderBy(e => e.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<HairTransplantEvent>> GetPendingEventsAsync()
    {
        var today = DateTime.UtcNow.Date;
        return await DbSet
            .Where(e => !e.IsCompleted && e.EndDate.Date >= today)
            .OrderBy(e => e.StartDate)
            .ToListAsync();
    }

    public async Task MarkEventAsCompletedAsync(Guid eventId)
    {
        var @event = await DbSet.FindAsync(eventId);
        if (@event != null)
        {
            @event.IsCompleted = true;
            @event.ModifiedAt = DateTime.UtcNow;
            await Context.SaveChangesAsync();
        }
    }
} 