using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Sync.Domain.Entities;
using HairCarePlus.Shared.Communication.Sync;
using HairCarePlus.Client.Patient.Features.Sync.Infrastructure;
using Microsoft.EntityFrameworkCore;
using HairCarePlus.Client.Patient.Infrastructure.Storage;

namespace HairCarePlus.Client.Patient.Features.Sync.Application;

public interface ISyncService
{
    Task SynchronizeAsync(CancellationToken cancellationToken);
}

public class SyncService : ISyncService
{
    private readonly IOutboxRepository _outbox;
    private readonly ISyncHttpClient _syncClient;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILastSyncVersionStore _versionStore;
    private readonly ISyncChangeApplier _applier;

    public SyncService(IOutboxRepository outbox,
                       ISyncHttpClient syncClient,
                       IDbContextFactory<AppDbContext> dbFactory,
                       ILastSyncVersionStore versionStore,
                       ISyncChangeApplier applier)
    {
        _outbox = outbox;
        _syncClient = syncClient;
        _dbFactory = dbFactory;
        _versionStore = versionStore;
        _applier = applier;
    }

    public async Task SynchronizeAsync(CancellationToken cancellationToken)
    {
        // 1. Gather local state -------------------------------------------
        var pendingItems = await _outbox.GetPendingItemsAsync();

        await using var db = _dbFactory.CreateDbContext();

        // Build pending lists BEFORE creating request
        var photoReportsToSend = new List<HairCarePlus.Shared.Communication.PhotoReportDto>();
        var photoCommentsToSend = new List<HairCarePlus.Shared.Communication.PhotoCommentDto>();

        foreach (var item in pendingItems)
        {
            switch (item.EntityType)
            {
                case "PhotoReport":
                    try
                    {
                        var reportDto = JsonSerializer.Deserialize<HairCarePlus.Shared.Communication.PhotoReportDto>(item.PayloadJson);
                        if (reportDto != null)
                            photoReportsToSend.Add(reportDto);
                    }
                    catch { /* ignore */ }
                    break;
                case "PhotoComment":
                    try
                    {
                        var commentDto = JsonSerializer.Deserialize<HairCarePlus.Shared.Communication.PhotoCommentDto>(item.PayloadJson);
                        if (commentDto != null)
                            photoCommentsToSend.Add(commentDto);
                    }
                    catch { }
                    break;
            }
        }

        var headers = await db.PhotoReports
                               .Select(r => new HairCarePlus.Shared.Communication.EntityHeaderDto
                               {
                                   Id = Guid.Parse(r.Id),
                                   // Use CaptureDate as last-modified proxy until UpdatedAt added
                                   ModifiedAtUtc = r.CaptureDate.ToUniversalTime()
                               })
                               .ToListAsync(cancellationToken);

        // 2. Build request -----------------------------------------------
        var lastVersion = await _versionStore.GetAsync();

        var req = new BatchSyncRequestDto
        {
            ClientId = "patient-app", // TODO: real device id
            LastSyncVersion = lastVersion,
            PhotoReportHeaders = headers,
            PhotoReports = photoReportsToSend.Count > 0 ? photoReportsToSend : null,
            PhotoComments = photoCommentsToSend.Count > 0 ? photoCommentsToSend : null
        };

        // 3. Call API ----------------------------------------------------
        var response = await _syncClient.PushAsync(req, cancellationToken);

        if (response == null) return; // network error

        // 4. Apply server changes locally --------------------------------
        await _applier.ApplyAsync(response);

        // 5. Update Outbox status ---------------------------------------
        if (pendingItems.Count > 0)
            await _outbox.UpdateStatusAsync(pendingItems.Select(i => i.Id), SyncStatus.Acked);

        // 6. Save new sync version --------------------------------------
        await _versionStore.SetAsync(response.NewSyncVersion);

        // 7. Handle NeedPhotoReports ------------------------------------
        if (response.NeedPhotoReports != null && response.NeedPhotoReports.Count > 0)
        {
            var neededIds = response.NeedPhotoReports.Select(g => g.ToString()).ToHashSet();
            var neededReports = await db.PhotoReports.Include(r => r.Comments)
                                                     .Where(r => neededIds.Contains(r.Id))
                                                     .ToListAsync(cancellationToken);

            foreach (var rep in neededReports)
            {
                var dto = new HairCarePlus.Shared.Communication.PhotoReportDto
                {
                    Id = Guid.Parse(rep.Id),
                    PatientId = Guid.Parse("00000000-0000-0000-0000-000000000000"), // TODO: real patient id
                    ImageUrl = rep.ImageUrl,
                    Date = rep.CaptureDate,
                    Notes = rep.DoctorComment ?? string.Empty,
                    Comments = rep.Comments.Select(c => new HairCarePlus.Shared.Communication.PhotoCommentDto
                    {
                        PhotoReportId = Guid.Parse(c.PhotoReportId),
                        AuthorId = Guid.Parse(c.AuthorId),
                        Text = c.Text,
                        CreatedAtUtc = c.CreatedAtUtc
                    }).ToList()
                };

                await _outbox.AddAsync(new OutboxItem
                {
                    EntityType = "PhotoReport",
                    PayloadJson = JsonSerializer.Serialize(dto),
                    LocalEntityId = rep.Id,
                    ModifiedAtUtc = DateTime.UtcNow
                });
            }
        }
    }
} 