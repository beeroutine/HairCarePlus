using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Sync.Domain.Entities;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using HairCarePlus.Client.Patient.Infrastructure.Storage;
using HairCarePlus.Shared.Communication;

namespace HairCarePlus.Client.Patient.Features.Sync.Infrastructure;

public class OutboxRepository : HairCarePlus.Shared.Communication.IOutboxRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public OutboxRepository(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task AddAsync(OutboxItemDto item)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = new OutboxItem
        {
            EntityType = item.EntityType,
            Payload = item.Payload,
            CreatedAtUtc = item.CreatedAtUtc,
            ModifiedAtUtc = item.ModifiedAtUtc,
            LocalEntityId = item.LocalEntityId,
            Status = item.Status,
            RetryCount = item.RetryCount
        };
        db.OutboxItems.Add(entity);
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<OutboxItemDto>> GetPendingItemsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.OutboxItems
            .Where(o => o.Status == HairCarePlus.Shared.Communication.OutboxStatus.Pending)
            .OrderBy(o => o.CreatedAtUtc)
            .Select(o => new OutboxItemDto
            {
                Id = o.Id,
                EntityType = o.EntityType,
                Payload = o.Payload,
                CreatedAtUtc = o.CreatedAtUtc,
                ModifiedAtUtc = o.ModifiedAtUtc,
                LocalEntityId = o.LocalEntityId,
                Status = o.Status,
                RetryCount = o.RetryCount
            })
            .ToListAsync();
    }

    public async Task UpdateStatusAsync(IEnumerable<int> ids, HairCarePlus.Shared.Communication.OutboxStatus status)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.OutboxItems
            .Where(o => ids.Contains(o.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, status));
    }

    public async Task DeleteAsync(IEnumerable<int> ids)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.OutboxItems
            .Where(o => ids.Contains(o.Id))
            .ExecuteDeleteAsync();
    }
} 