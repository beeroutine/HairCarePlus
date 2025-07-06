using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Sync.Domain.Entities;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using HairCarePlus.Client.Patient.Infrastructure.Storage;

namespace HairCarePlus.Client.Patient.Features.Sync.Infrastructure;

public interface IOutboxRepository
{
    Task AddAsync(OutboxItem item);
    Task<IReadOnlyList<OutboxItem>> GetPendingItemsAsync();
    Task UpdateStatusAsync(IEnumerable<int> ids, SyncStatus status);
    Task DeleteAsync(IEnumerable<int> ids);
}

public class OutboxRepository : IOutboxRepository
{
    private readonly AppDbContext _db;

    public OutboxRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(OutboxItem item)
    {
        _db.OutboxItems.Add(item);
        return _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<OutboxItem>> GetPendingItemsAsync()
    {
        return await _db.OutboxItems
            .Where(o => o.Status == SyncStatus.Pending)
            .OrderBy(o => o.CreatedAtUtc)
            .ToListAsync();
    }

    public Task UpdateStatusAsync(IEnumerable<int> ids, SyncStatus status)
    {
        return _db.OutboxItems
            .Where(o => ids.Contains(o.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, status));
    }

    public Task DeleteAsync(IEnumerable<int> ids)
    {
        return _db.OutboxItems
            .Where(o => ids.Contains(o.Id))
            .ExecuteDeleteAsync();
    }
} 