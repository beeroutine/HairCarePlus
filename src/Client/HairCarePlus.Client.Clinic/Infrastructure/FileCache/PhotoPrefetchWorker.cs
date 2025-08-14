using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using HairCarePlus.Client.Clinic.Infrastructure.Storage;

namespace HairCarePlus.Client.Clinic.Infrastructure.FileCache;

public sealed class PhotoPrefetchWorker : BackgroundService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IFileCacheService _fileCache;
    private readonly ILogger<PhotoPrefetchWorker> _logger;

    public PhotoPrefetchWorker(IDbContextFactory<AppDbContext> dbFactory,
                               IFileCacheService fileCache,
                               ILogger<PhotoPrefetchWorker> logger)
    {
        _dbFactory = dbFactory;
        _fileCache = fileCache;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PrefetchCycleAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Photo prefetch cycle failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task PrefetchCycleAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var uncached = await db.PhotoReports
                               .Where(r => string.IsNullOrEmpty(r.LocalPath))
                               .Select(r => new { r.Id, r.ImageUrl })
                               .Take(10)
                               .ToListAsync(ct);
        if (uncached.Count == 0) return;

        var urls = uncached.Select(u => u.ImageUrl)
                           .Where(u => !string.IsNullOrWhiteSpace(u) &&
                                       (u.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || u.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                           .ToList();
        await _fileCache.PrefetchAsync(urls, ct);

        foreach (var item in uncached)
        {
            if (!string.IsNullOrWhiteSpace(item.ImageUrl) &&
                (item.ImageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || item.ImageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
            {
                var path = await _fileCache.GetLocalPathAsync(item.ImageUrl, ct);
                var entity = await db.PhotoReports.FirstAsync(r => r.Id == item.Id, ct);
                entity.LocalPath = path;
            }
        }
        await db.SaveChangesAsync(ct);
    }
} 