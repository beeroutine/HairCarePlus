using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HairCarePlus.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text.Json;
using HairCarePlus.Shared.Communication;

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
        var now = DateTime.UtcNow;

        // Base query: not expired and targeted to this receiver, not yet delivered to it
        var query = _db.DeliveryQueue.Where(d => d.ExpiresAtUtc > now &&
                                                 (d.ReceiversMask & receiverMask) != 0 &&
                                                 (d.DeliveredMask & receiverMask) == 0);

        // Optional scope by patient
        if (patientId != Guid.Empty)
        {
            query = query.Where(d => d.PatientId == patientId);
        }

        var candidates = await query
            .OrderBy(d => d.EntityType == nameof(PhotoReportSetDto) ? 0 : d.EntityType == "PhotoReport" ? 0 : d.EntityType == "PhotoComment" ? 1 : 2)
            .ThenBy(d => d.CreatedAt)
            .ToListAsync();

        // Filter out photo packets that reference files that do not exist anymore
        // This can happen after a "clean" server restart when uploads folder is empty.
        var uploadsDir = Path.Combine(AppContext.BaseDirectory, "uploads");
        bool FileExistsByUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            try
            {
                var name = Path.GetFileName(new Uri(url, UriKind.Absolute).LocalPath);
                var path = Path.Combine(uploadsDir, name);
                return File.Exists(path);
            }
            catch { return false; }
        }

        var filtered = new List<DeliveryQueue>(candidates.Count);
        foreach (var item in candidates)
        {
            try
            {
                if (item.EntityType == nameof(PhotoReportDto) || item.EntityType == "PhotoReport")
                {
                    var dto = JsonSerializer.Deserialize<PhotoReportDto>(item.PayloadJson);
                    if (dto == null || !FileExistsByUrl(dto.ImageUrl))
                        continue; // skip broken packet
                }
                else if (item.EntityType == nameof(PhotoReportSetDto))
                {
                    var set = JsonSerializer.Deserialize<PhotoReportSetDto>(item.PayloadJson);
                    if (set?.Items == null || set.Items.Count == 0 || !set.Items.All(i => FileExistsByUrl(i.ImageUrl)))
                        continue; // skip if any photo file is missing
                }
            }
            catch
            {
                // If payload cannot be parsed, skip to be safe
                continue;
            }

            filtered.Add(item);
        }

        return filtered;
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
                // Remove associated files according to entity type (ephemeral policy)
                try
                {
                    var uploadsDir = Path.Combine(AppContext.BaseDirectory, "uploads");
                    if (row.EntityType == "PhotoReport")
                    {
                        var dto = JsonSerializer.Deserialize<PhotoReportDto>(row.PayloadJson);
                        var url = dto?.ImageUrl;
                        if (!string.IsNullOrWhiteSpace(url))
                        {
                            var fileName = Path.GetFileName(new Uri(url, UriKind.Absolute).LocalPath);
                            var path = Path.Combine(uploadsDir, fileName);
                            if (File.Exists(path)) File.Delete(path);
                        }
                    }
                    else if (row.EntityType == nameof(PhotoReportSetDto))
                    {
                        var set = JsonSerializer.Deserialize<PhotoReportSetDto>(row.PayloadJson);
                        if (set?.Items != null)
                        {
                            foreach (var it in set.Items)
                            {
                                var url = it.ImageUrl;
                                if (string.IsNullOrWhiteSpace(url)) continue;
                                var fileName = Path.GetFileName(new Uri(url, UriKind.Absolute).LocalPath);
                                var path = Path.Combine(uploadsDir, fileName);
                                if (File.Exists(path)) File.Delete(path);
                            }
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(row.BlobUrl))
                    {
                        var path = Path.Combine(uploadsDir, Path.GetFileName(row.BlobUrl));
                        if (File.Exists(path)) File.Delete(path);
                    }
                }
                catch { /* best effort */ }

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