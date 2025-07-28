using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HairCarePlus.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.IO;

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
        var query = _db.DeliveryQueue.AsQueryable();

        if (patientId != Guid.Empty)
        {
            query = query.Where(d => d.PatientId == patientId);
        }

        return await query.Where(d => (d.ReceiversMask & receiverMask) != 0 &&
                                       (d.DeliveredMask & receiverMask) == 0)
                          .ToListAsync();
    }

    public async Task AckAsync(IEnumerable<Guid> ids, byte receiverMask)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return;

        var rows = await _db.DeliveryQueue.Where(d => idList.Contains(d.Id)).ToListAsync();
        foreach (var row in rows)
        {
            // mark bit
            row.DeliveredMask = (byte)(row.DeliveredMask | receiverMask);

            // if delivered to all receivers -> mark for removal
            if ((row.DeliveredMask & row.ReceiversMask) == row.ReceiversMask)
            {
                // remove associated blob if any
                if (!string.IsNullOrWhiteSpace(row.BlobUrl))
                {
                    try
                    {
                        var root = AppContext.BaseDirectory;
                        var path = Path.Combine(root, "uploads", Path.GetFileName(row.BlobUrl));
                        if (File.Exists(path))
                            File.Delete(path);
                    }
                    catch { /* best effort */ }
                }

                _db.DeliveryQueue.Remove(row);
            }
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