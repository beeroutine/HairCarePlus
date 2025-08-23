using HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;
using HairCarePlus.Client.Patient.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using HairCarePlus.Shared.Common.CQRS;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;
using HairCarePlus.Client.Patient.Infrastructure.Services.Interfaces;
using Microsoft.Maui.Storage;
using System.IO;

namespace HairCarePlus.Client.Patient.Features.Progress.Application.Queries;

public sealed class GetLocalPhotoReportsQueryHandler : IQueryHandler<GetLocalPhotoReportsQuery, IEnumerable<ProgressFeedItem>>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IProfileService _profileService;

    public GetLocalPhotoReportsQueryHandler(IDbContextFactory<AppDbContext> dbFactory, IProfileService profileService)
    {
        _dbFactory = dbFactory;
        _profileService = profileService;
    }

    public async Task<IEnumerable<ProgressFeedItem>> HandleAsync(GetLocalPhotoReportsQuery request, CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var patientId = _profileService.PatientId.ToString();

        var allReports = await db.PhotoReports
            .AsNoTracking()
            .Where(r => r.PatientId == patientId || string.IsNullOrEmpty(r.PatientId))
            .OrderByDescending(r => r.CaptureDate)
            .ToListAsync(cancellationToken);

        // Support both absolute and relative LocalPath values (legacy rows stored absolute paths)
        string ToFullPath(string storedPath)
        {
            if (Path.IsPathRooted(storedPath))
                return storedPath;
            return Path.Combine(FileSystem.AppDataDirectory, "Media", storedPath);
        }

        // Build a list with resolved local file paths (supports legacy rows where LocalPath was null
        // but ImageUrl stored an absolute local file path)
        var withPaths = allReports
            .Select(r => new
            {
                Report = r,
                Path = ResolveLocalFilePath(r)
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Path) && File.Exists(x.Path!))
            .ToList();

        string? ResolveLocalFilePath(HairCarePlus.Client.Patient.Features.Sync.Domain.Entities.PhotoReportEntity report)
        {
            // Prefer LocalPath when present
            if (!string.IsNullOrWhiteSpace(report.LocalPath))
            {
                var full = ToFullPath(report.LocalPath!);
                if (File.Exists(full))
                    return full;
            }

            // Fallback: if ImageUrl is a local absolute file (pre-fix rows), use it
            if (!string.IsNullOrWhiteSpace(report.ImageUrl)
                && !report.ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                && Path.IsPathRooted(report.ImageUrl)
                && File.Exists(report.ImageUrl))
            {
                return report.ImageUrl;
            }

            return null;
        }

        var feedItems = withPaths
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Report.SetId) ? DateOnly.FromDateTime(x.Report.CaptureDate).ToString() : x.Report.SetId!)
            .Select(group =>
            {
                var first = group.First().Report;
                var photos = group
                    .Select(x => new ProgressPhoto {
                        ReportId = x.Report.Id,
                        LocalPath = x.Path!,
                        CapturedAt = x.Report.CaptureDate,
                        Zone = x.Report.Zone })
                    .ToList();

                // Compute Day N similar to clinic feed (Day 1 = earliest date in this local set)
                var groupDate = DateOnly.FromDateTime(first.CaptureDate);
                var earliestDate = withPaths.Any() ? DateOnly.FromDateTime(withPaths.Min(x => x.Report.CaptureDate)) : groupDate;
                var dayIndex = (groupDate.DayNumber - earliestDate.DayNumber) + 1;
                var feedItem = new ProgressFeedItem(
                    Date: groupDate,
                    Title: $"Day {dayIndex}",
                    Description: first.DoctorComment ?? string.Empty,
                    Photos: photos,
                    ActiveRestrictions: new List<RestrictionTimer>() // TODO: Load actual restrictions for that day
                );
                // Surface doctor's comment in a dedicated property used by the card view
                var summary = group.Select(g => g.Report.DoctorComment).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
                feedItem.DoctorReportSummary = string.IsNullOrWhiteSpace(summary) ? null : summary;
                return feedItem;
            })
            .ToList();

        return feedItems;
    }
} 