using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HairCarePlus.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HairCarePlus.Server.Infrastructure.Data.Repositories;

public interface IDeliveryQueueRepository
{
    Task AddRangeAsync(IEnumerable<DeliveryQueue> items);
    Task<List<DeliveryQueue>> GetPendingForReceiverAsync(Guid patientId, byte receiverMask);
    Task AckAsync(IEnumerable<Guid> ids, byte receiverMask);
    Task RemoveExpiredAsync();
}

public class DeliveryQueueRepository : IDeliveryQueueRepository
{
    private readonly AppDbContext _db;

    public DeliveryQueueRepository(AppDbContext db) => _db = db;

    public async Task AddRangeAsync(IEnumerable<DeliveryQueue> items)
    {
        await _db.DeliveryQueue.AddRangeAsync(items);
        await _db.SaveChangesAsync();
    }

    public async Task<List<DeliveryQueue>> GetPendingForReceiverAsync(Guid patientId, byte receiverMask)
    {
        return await _db.DeliveryQueue
                        .Where(d => d.PatientId == patientId &&
                                    (d.ReceiversMask & receiverMask) != 0 &&
                                    (d.DeliveredMask & receiverMask) == 0)
                        .ToListAsync();
    }

    public async Task AckAsync(IEnumerable<Guid> ids, byte receiverMask)
    {
        var set = ids.ToList();
        if (set.Count == 0) return;

        var rows = await _db.DeliveryQueue.Where(d => set.Contains(d.Id)).ToListAsync();
        foreach (var row in rows)
        {
            row.DeliveredMask = (byte)(row.DeliveredMask | receiverMask);
        }
        await _db.SaveChangesAsync();
    }

    public async Task RemoveExpiredAsync()
    {
        var now = DateTime.UtcNow;
        var expired = await _db.DeliveryQueue
                               .Where(d => d.ExpiresAtUtc < now || d.DeliveredMask == d.ReceiversMask)
                               .ToListAsync();
        if (expired.Count == 0) return;
        _db.DeliveryQueue.RemoveRange(expired);
        await _db.SaveChangesAsync();
    }
} 