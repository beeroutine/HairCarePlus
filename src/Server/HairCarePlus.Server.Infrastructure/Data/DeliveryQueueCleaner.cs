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

                // Clean up associated files BEFORE removing the DeliveryQueue records
                foreach (var item in itemsToRemove.Where(i => i.EntityType == "PhotoReport"))
                {
                    try
                    {
                        var dto = JsonSerializer.Deserialize<HairCarePlus.Shared.Communication.PhotoReportDto>(item.PayloadJson);
                        if (dto != null && !string.IsNullOrWhiteSpace(dto.ImageUrl))
                        {
                            var fileName = Path.GetFileName(new Uri(dto.ImageUrl, UriKind.Absolute).LocalPath);
                            var uploadsDir = Path.Combine(AppContext.BaseDirectory, "uploads");
                            var physicalPath = Path.Combine(uploadsDir, fileName);
                            
                            if (File.Exists(physicalPath))
                            {
                                File.Delete(physicalPath);
                                _logger.LogInformation("[DeliveryQueueCleaner] Deleted orphaned file {File}", physicalPath);
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