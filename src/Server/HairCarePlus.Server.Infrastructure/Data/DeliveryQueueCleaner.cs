using System;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Server.Infrastructure.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace HairCarePlus.Server.Infrastructure.Data;

public class DeliveryQueueCleaner : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<DeliveryQueueCleaner> _logger;

    public DeliveryQueueCleaner(IServiceProvider sp, ILogger<DeliveryQueueCleaner> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                // Find DeliveryQueue items that will be removed (expired or fully delivered)
                var now = DateTime.UtcNow;
                var itemsToRemove = await db.DeliveryQueue
                    .Where(d => d.ExpiresAtUtc < now || d.DeliveredMask == d.ReceiversMask)
                    .ToListAsync();

                // Clean up associated files BEFORE removing the DeliveryQueue records (PhotoReport & PhotoReportSet)
                foreach (var item in itemsToRemove)
                {
                    try
                    {
                        var uploadsDir = Path.Combine(AppContext.BaseDirectory, "uploads");
                        if (item.EntityType == "PhotoReport")
                        {
                            var dto = JsonSerializer.Deserialize<HairCarePlus.Shared.Communication.PhotoReportDto>(item.PayloadJson);
                            if (dto != null && !string.IsNullOrWhiteSpace(dto.ImageUrl))
                            {
                                var fileName = Path.GetFileName(new Uri(dto.ImageUrl, UriKind.Absolute).LocalPath);
                                var physicalPath = Path.Combine(uploadsDir, fileName);
                                if (File.Exists(physicalPath)) { File.Delete(physicalPath); _logger.LogInformation("[DeliveryQueueCleaner] Deleted file {File}", physicalPath); }
                            }
                        }
                        else if (item.EntityType == nameof(HairCarePlus.Shared.Communication.PhotoReportSetDto))
                        {
                            var set = JsonSerializer.Deserialize<HairCarePlus.Shared.Communication.PhotoReportSetDto>(item.PayloadJson);
                            if (set?.Items != null)
                            {
                                foreach (var it in set.Items)
                                {
                                    if (string.IsNullOrWhiteSpace(it.ImageUrl)) continue;
                                    var fileName = Path.GetFileName(new Uri(it.ImageUrl, UriKind.Absolute).LocalPath);
                                    var physicalPath = Path.Combine(uploadsDir, fileName);
                                    if (File.Exists(physicalPath)) { File.Delete(physicalPath); _logger.LogInformation("[DeliveryQueueCleaner] Deleted file {File}", physicalPath); }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[DeliveryQueueCleaner] Failed to clean file for DeliveryQueue item {Id}", item.Id);
                    }
                }

                // Now use repository to remove expired/delivered items.
                // Additionally, enforce ephemeral policy: hard-delete any stale PhotoReports from main DB older than TTL.
                var repo = scope.ServiceProvider.GetRequiredService<IDeliveryQueueRepository>();
                await repo.RemoveExpiredAsync();

                if (itemsToRemove.Count > 0)
                {
                    _logger.LogInformation("[DeliveryQueueCleaner] Cleaned up {Count} expired/delivered items", itemsToRemove.Count);
                }

                // Hard-delete historical PhotoReports beyond TTL to avoid server-side history buildup
                var ttlDays = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DeliveryOptions>>().Value.PhotoReportTtlDays;
                var threshold = DateTime.UtcNow.AddDays(-ttlDays);
                var staleReports = await db.PhotoReports.Where(r => (r.UpdatedAt ?? r.CreatedAt) < threshold).ToListAsync();
                if (staleReports.Count > 0)
                {
                    db.PhotoReports.RemoveRange(staleReports);
                    await db.SaveChangesAsync();
                    _logger.LogInformation("[DeliveryQueueCleaner] Hard-deleted {Count} stale PhotoReports older than {Days} days", staleReports.Count, ttlDays);
                }

                // Optional: delete orphaned files left in uploads (no corresponding queue items)
                try
                {
                    var uploads = Path.Combine(AppContext.BaseDirectory, "uploads");
                    if (Directory.Exists(uploads))
                    {
                        var keepSet = itemsToRemove.SelectMany(i =>
                        {
                            try
                            {
                                if (i.EntityType == "PhotoReport")
                                {
                                    var dto = JsonSerializer.Deserialize<HairCarePlus.Shared.Communication.PhotoReportDto>(i.PayloadJson);
                                    return dto != null && !string.IsNullOrWhiteSpace(dto.ImageUrl)
                                        ? new[] { Path.GetFileName(new Uri(dto.ImageUrl, UriKind.Absolute).LocalPath) }
                                        : Array.Empty<string>();
                                }
                                if (i.EntityType == nameof(HairCarePlus.Shared.Communication.PhotoReportSetDto))
                                {
                                    var set = JsonSerializer.Deserialize<HairCarePlus.Shared.Communication.PhotoReportSetDto>(i.PayloadJson);
                                    return set?.Items?.Where(x => !string.IsNullOrWhiteSpace(x.ImageUrl))
                                               .Select(x => Path.GetFileName(new Uri(x.ImageUrl, UriKind.Absolute).LocalPath))
                                               .ToArray() ?? Array.Empty<string>();
                                }
                            }
                            catch { }
                            return Array.Empty<string>();
                        }).ToHashSet(StringComparer.OrdinalIgnoreCase);

                        foreach (var file in Directory.EnumerateFiles(uploads))
                        {
                            var name = Path.GetFileName(file);
                            if (!keepSet.Contains(name))
                            {
                                try { File.Delete(file); _logger.LogInformation("[DeliveryQueueCleaner] Deleted orphan file {File}", file); } catch { }
                            }
                        }
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[DeliveryQueueCleaner] Cleanup cycle failed");
            }
            
            // Run cleanup every hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
} 