using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Client.Clinic.Features.Sync.Domain.Entities;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using HairCarePlus.Client.Clinic.Infrastructure.Storage;
using HairCarePlus.Shared.Communication;

namespace HairCarePlus.Client.Clinic.Features.Sync.Infrastructure;

public class OutboxRepository : IOutboxRepository
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
            Status = item.Status,
            RetryCount = item.RetryCount,
            LocalEntityId = item.LocalEntityId,
            ModifiedAtUtc = item.ModifiedAtUtc
        };
        
        db.OutboxItems.Add(entity);
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<OutboxItemDto>> GetPendingItemsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entities = await db.OutboxItems
            .Where(o => o.Status == OutboxStatus.Pending)
            .OrderBy(o => o.CreatedAtUtc)
            .ToListAsync();

        return entities.Select(e => new OutboxItemDto
        {
            Id = e.Id,
            EntityType = e.EntityType,
            Payload = e.Payload,
            CreatedAtUtc = e.CreatedAtUtc,
            Status = e.Status,
            RetryCount = e.RetryCount,
            LocalEntityId = e.LocalEntityId,
            ModifiedAtUtc = e.ModifiedAtUtc
        }).ToList();
    }

    public async Task UpdateStatusAsync(IEnumerable<int> ids, OutboxStatus status)
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