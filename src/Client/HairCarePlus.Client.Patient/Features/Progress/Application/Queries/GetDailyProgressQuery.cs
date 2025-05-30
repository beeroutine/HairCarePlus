using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Shared.Common.CQRS;
using HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;
using HairCarePlus.Client.Patient.Infrastructure.Media;
using Microsoft.Extensions.Logging;
using HairCarePlus.Client.Patient.Features.Progress.Services.Interfaces;

namespace HairCarePlus.Client.Patient.Features.Progress.Application.Queries;

/// <summary>
/// Возвращает сводную информацию о прогрессе (фото, процедуры, ограничения, AI-отчёт) за конкретную дату.
/// </summary>
public sealed record GetDailyProgressQuery(DateOnly Date) : IQuery<DailyProgress>;

/// <summary>
/// DTO результата запроса.
/// </summary>
/// <param name="RestrictionTimers">Актуальные таймеры ограничений.</param>
/// <param name="Photos">Снимки, сделанные за дату.</param>
/// <param name="Procedures">Список процедур и отметок выполнения.</param>
/// <param name="AiReport">Отчёт AI (может отсутствовать).</param>
public sealed record DailyProgress(
    IReadOnlyList<RestrictionTimer> RestrictionTimers,
    IReadOnlyList<ProgressPhoto> Photos,
    IReadOnlyList<ProcedureCheck> Procedures,
    AIReport? AiReport);

/// <summary>
/// Handler – MVP-реализация: читает локальный кэш фото, отдаёт заглушечные ограничения.
/// </summary>
public sealed class GetDailyProgressHandler : IQueryHandler<GetDailyProgressQuery, DailyProgress>
{
    private readonly IMediaFileSystemService _fs;
    private readonly IRestrictionService _restrictionService;
    private readonly ILogger<GetDailyProgressHandler> _logger;

    public GetDailyProgressHandler(IMediaFileSystemService fs, IRestrictionService restrictionService, ILogger<GetDailyProgressHandler> logger)
    {
        _fs = fs;
        _restrictionService = restrictionService;
        _logger = logger;
    }

    public async Task<DailyProgress> HandleAsync(GetDailyProgressQuery query, CancellationToken cancellationToken = default)
    {
        var date = query.Date;

        // 1. Restriction timers
        IReadOnlyList<RestrictionTimer> timers;
        try
        {
            timers = await _restrictionService.GetActiveRestrictionsAsync(cancellationToken);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to load restrictions");
            timers = new List<RestrictionTimer>();
        }

        // 2. Photos
        var photos = new List<ProgressPhoto>();
        try
        {
            var cacheDir = await _fs.GetCacheDirectoryAsync();
            var photoDir = System.IO.Path.Combine(cacheDir, "captured_photos");
            if (System.IO.Directory.Exists(photoDir))
            {
                var files = System.IO.Directory.GetFiles(photoDir, "photo_*" + date.ToString("yyyyMMdd") + "_*.jpg");
                foreach (var file in files)
                {
                    photos.Add(new ProgressPhoto
                    {
                        LocalPath = file,
                        CapturedAt = System.IO.File.GetCreationTime(file),
                        Zone = GuessZone(file),
                        AiScore = 0
                    });
                }
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error collecting progress photos");
        }

        // 3. Procedures – stub (none)
        var procedures = new List<ProcedureCheck>();

        // 4. AI report – stub null
        AIReport? report = null;

        return new DailyProgress(timers, photos, procedures, report);
    }

    private static PhotoZone GuessZone(string path)
    {
        if (path.Contains("front")) return PhotoZone.Front;
        if (path.Contains("top")) return PhotoZone.Top;
        if (path.Contains("back")) return PhotoZone.Back;
        return PhotoZone.Front;
    }
} 