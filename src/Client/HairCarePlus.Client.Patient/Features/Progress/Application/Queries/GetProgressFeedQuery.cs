using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Shared.Common.CQRS;
using HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;
using HairCarePlus.Client.Patient.Infrastructure.Media;
using HairCarePlus.Client.Patient.Features.Progress.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Client.Patient.Features.Progress.Application.Queries;

/// <summary>
/// Returns a chronological feed of progress information for a given date range.
/// </summary>
/// <param name="From">Inclusive start date (older bound).</param>
/// <param name="To">Inclusive end date (newest bound).</param>
public sealed record GetProgressFeedQuery(DateOnly From, DateOnly To) : IQuery<IReadOnlyList<ProgressFeedItem>>;

/// <summary>
/// Handler that assembles a feed from local photos and stub data for restrictions / AI reports.
/// </summary>
public sealed class GetProgressFeedHandler : IQueryHandler<GetProgressFeedQuery, IReadOnlyList<ProgressFeedItem>>
{
    private readonly IMediaFileSystemService _fs;
    private readonly IRestrictionService _restrictionService;
    private readonly ILogger<GetProgressFeedHandler> _logger;

    public GetProgressFeedHandler(
        IMediaFileSystemService fs,
        IRestrictionService restrictionService,
        ILogger<GetProgressFeedHandler> logger)
    {
        _fs = fs;
        _restrictionService = restrictionService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProgressFeedItem>> HandleAsync(GetProgressFeedQuery query, CancellationToken cancellationToken = default)
    {
        if (query.From > query.To)
            throw new ArgumentException("The 'From' date must be earlier than or equal to 'To'.");

        var feedItems = new List<ProgressFeedItem>();

        // Get today restrictions (stub). We'll derive historical values from these.
        IReadOnlyList<RestrictionTimer> todayRestrictions;
        try
        {
            todayRestrictions = await _restrictionService.GetActiveRestrictionsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load restrictions");
            todayRestrictions = new List<RestrictionTimer>();
        }

        var cacheDir = await _fs.GetCacheDirectoryAsync();
        var photoDir = Path.Combine(cacheDir, "captured_photos");

        for (var date = query.To; date >= query.From; date = date.AddDays(-1))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var photos = new List<ProgressPhoto>();
            try
            {
                if (Directory.Exists(photoDir))
                {
                    var patternDate = date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                    var files = Directory.GetFiles(photoDir, $"photo_*{patternDate}_*.jpg");
                    foreach (var file in files)
                    {
                        photos.Add(new ProgressPhoto
                        {
                            LocalPath = file,
                            CapturedAt = File.GetCreationTime(file),
                            Zone = GuessZone(file),
                            AiScore = 0
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while gathering photos for date {Date}", date);
            }

            // Derive restriction timers state for this historical date
            var derivedRestrictions = todayRestrictions
                .Select(r => new RestrictionTimer { Title = r.Title, DaysRemaining = r.DaysRemaining + (query.To.DayNumber - date.DayNumber) })
                .ToList();

            // Produce artificial AI feedback placeholder when photos exist
            AIReport? aiReport = null;
            if (photos.Count > 0)
            {
                // Simple heuristic: newer dates get slightly higher score
                var daysDelta = (query.To.DayNumber - date.DayNumber);
                var scoreBase = 60;
                var score = Math.Clamp(scoreBase - daysDelta, 50, 90);
                aiReport = new AIReport(date, score, "_Awaiting official AI analysis. Auto-generated score for preview purposes._");
            }

            feedItems.Add(new ProgressFeedItem(date, photos, derivedRestrictions, aiReport));
        }

        return feedItems;
    }

    private static PhotoZone GuessZone(string path)
    {
        path = path.ToLowerInvariant();
        if (path.Contains("front")) return PhotoZone.Front;
        if (path.Contains("top")) return PhotoZone.Top;
        if (path.Contains("back")) return PhotoZone.Back;
        return PhotoZone.Front;
    }
} 