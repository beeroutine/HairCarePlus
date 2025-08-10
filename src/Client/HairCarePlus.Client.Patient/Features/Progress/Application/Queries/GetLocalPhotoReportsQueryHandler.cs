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
        
        var allReports = await db.PhotoReports
            .AsNoTracking()
            .Where(r => !string.IsNullOrEmpty(r.LocalPath))
            .OrderByDescending(r => r.CaptureDate)
            .ToListAsync(cancellationToken);

        // Support both absolute and relative LocalPath values (legacy rows stored absolute paths)
        string ToFullPath(string storedPath)
        {
            if (Path.IsPathRooted(storedPath))
                return storedPath;
            return Path.Combine(FileSystem.AppDataDirectory, "Media", storedPath);
        }

        // filter out rows whose files are missing
        allReports = allReports.Where(r => File.Exists(ToFullPath(r.LocalPath!))).ToList();

        var feedItems = allReports
            .GroupBy(report => DateOnly.FromDateTime(report.CaptureDate))
            .Select(group =>
            {
                var report = group.First();
                var photos = group
                    .Select(r => new ProgressPhoto {
                        LocalPath = ToFullPath(r.LocalPath ?? string.Empty),
                        CapturedAt = r.CaptureDate,
                        Zone = r.Zone })
                    .ToList();

                var feedItem = new ProgressFeedItem(
                    Date: group.Key,
                Title: "Фотоотчет",
                    Description: report.DoctorComment ?? string.Empty,
                    Photos: photos,
                    ActiveRestrictions: new List<RestrictionTimer>() // TODO: Load actual restrictions for that day
                );
                return feedItem;
            })
            .ToList();

        return feedItems;
    }
} 