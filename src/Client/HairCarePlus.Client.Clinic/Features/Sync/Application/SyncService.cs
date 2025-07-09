using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Client.Clinic.Features.Sync.Domain.Entities;
using HairCarePlus.Client.Clinic.Features.Sync.Infrastructure;
using HairCarePlus.Client.Clinic.Infrastructure.Storage;
using HairCarePlus.Shared.Communication.Sync;
using Microsoft.EntityFrameworkCore;

namespace HairCarePlus.Client.Clinic.Features.Sync.Application;

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

    private readonly List<Guid> _pendingAckIds = new();

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
        // 1. Gather pending local changes
        var pendingItems = await _outbox.GetPendingItemsAsync();

        await using var db = _dbFactory.CreateDbContext();

        var photoReportsToSend = new List<HairCarePlus.Shared.Communication.PhotoReportDto>();
        var photoCommentsToSend = new List<HairCarePlus.Shared.Communication.PhotoCommentDto>();

        foreach (var item in pendingItems)
        {
            switch (item.EntityType)
            {
                case "PhotoReport":
                    try
                    {
                        var dto = JsonSerializer.Deserialize<HairCarePlus.Shared.Communication.PhotoReportDto>(item.PayloadJson);
                        if (dto != null)
                            photoReportsToSend.Add(dto);
                    }
                    catch { /* ignore */ }
                    break;
                case "PhotoComment":
                    try
                    {
                        var dto = JsonSerializer.Deserialize<HairCarePlus.Shared.Communication.PhotoCommentDto>(item.PayloadJson);
                        if (dto != null)
                            photoCommentsToSend.Add(dto);
                    }
                    catch { /* ignore */ }
                    break;
            }
        }

        // prepare headers (clinic cares about patient-side reports, but still send its own)
        var reportHeaders = await db.PhotoReports
                                     .Select(r => new HairCarePlus.Shared.Communication.EntityHeaderDto
                                     {
                                         Id = Guid.Parse(r.Id),
                                         ModifiedAtUtc = r.Date.ToUniversalTime()
                                     })
                                     .ToListAsync(cancellationToken);

        var lastVersion = await _versionStore.GetAsync();

        var request = new BatchSyncRequestDto
        {
            ClientId = "clinic-app", // TODO: unique device id
            LastSyncVersion = lastVersion,
            PhotoReportHeaders = reportHeaders,
            PhotoReports = photoReportsToSend.Count > 0 ? photoReportsToSend : null,
            PhotoComments = photoCommentsToSend.Count > 0 ? photoCommentsToSend : null,
            AckIds = _pendingAckIds.Count > 0 ? _pendingAckIds : null
        };

        var response = await _syncClient.PushAsync(request, cancellationToken);
        if (response == null) return; // network error

        // 2. Process incoming packets
        if (response.Packets != null)
        {
            await using var db2 = _dbFactory.CreateDbContext();
            foreach (var packet in response.Packets)
            {
                switch (packet.EntityType)
                {
                    case "PhotoReport":
                        try
                        {
                            var dto = JsonSerializer.Deserialize<HairCarePlus.Shared.Communication.PhotoReportDto>(packet.PayloadJson);
                            if (dto != null && await db2.PhotoReports.FirstOrDefaultAsync(r => r.Id == dto.Id.ToString()) == null)
                            {
                                var entity = new Domain.Entities.PhotoReportEntity
                                {
                                    Id = dto.Id.ToString(),
                                    PatientId = dto.PatientId.ToString(),
                                    ImageUrl = dto.ImageUrl,
                                    Date = dto.Date,
                                    DoctorComment = dto.Notes
                                };
                                await db2.PhotoReports.AddAsync(entity, cancellationToken);
                                _pendingAckIds.Add(packet.Id);
                            }
                        }
                        catch { /* ignore */ }
                        break;
                    case "Restriction":
                        try
                        {
                            var dto = JsonSerializer.Deserialize<HairCarePlus.Shared.Communication.RestrictionDto>(packet.PayloadJson);
                            if (dto != null)
                            {
                                var id = dto.Id.ToString();
                                var entity = await db2.Restrictions.FirstOrDefaultAsync(r => r.Id == id);
                                if (entity == null)
                                {
                                    entity = new Domain.Entities.RestrictionEntity
                                    {
                                        Id = id,
                                        PatientId = dto.PatientId.ToString(),
                                        Type = (int)dto.Type,
                                        StartUtc = dto.StartUtc,
                                        EndUtc = dto.EndUtc,
                                        IsActive = dto.IsActive
                                    };
                                    await db2.Restrictions.AddAsync(entity, cancellationToken);
                                }
                                else
                                {
                                    entity.Type = (int)dto.Type;
                                    entity.StartUtc = dto.StartUtc;
                                    entity.EndUtc = dto.EndUtc;
                                    entity.IsActive = dto.IsActive;
                                }
                                _pendingAckIds.Add(packet.Id);
                            }
                        }
                        catch { /* ignore */ }
                        break;
                }
            }
            await db2.SaveChangesAsync(cancellationToken);
        }

        // 3. Apply server changes via applier
        await _applier.ApplyAsync(response);

        // 4. Update outbox statuses
        if (pendingItems.Count > 0)
            await _outbox.UpdateStatusAsync(pendingItems.Select(i => i.Id), SyncStatus.Acked);

        // 5. Save new version
        await _versionStore.SetAsync(response.NewSyncVersion);

        // 6. clear ack list
        _pendingAckIds.Clear();

        // 7. Handle NeedPhotoReports -> enqueue missing ones
        if (response.NeedPhotoReports != null && response.NeedPhotoReports.Count > 0)
        {
            var neededIds = response.NeedPhotoReports.Select(g => g.ToString()).ToHashSet();
            var neededReports = await db.PhotoReports.Include(p => p.Comments)
                                                     .Where(p => neededIds.Contains(p.Id))
                                                     .ToListAsync(cancellationToken);

            foreach (var rep in neededReports)
            {
                var dto = new HairCarePlus.Shared.Communication.PhotoReportDto
                {
                    Id = Guid.Parse(rep.Id),
                    PatientId = Guid.Parse(rep.PatientId),
                    ImageUrl = rep.ImageUrl,
                    Date = rep.Date,
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