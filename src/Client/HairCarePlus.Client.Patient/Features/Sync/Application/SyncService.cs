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
using HairCarePlus.Client.Patient.Features.Progress.Services.Interfaces;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<SyncService> _logger;
    private readonly HairCarePlus.Client.Patient.Features.Progress.Services.Interfaces.IRestrictionService _restrictionService;
    private readonly List<Guid> _pendingAckIds = new();
    // TODO: replace with profile-backed patient identifier when real auth implemented
    private static readonly Guid _patientId = Guid.Parse("8f8c7e0b-1234-4e78-a8cc-ff0011223344");

    public SyncService(IOutboxRepository outbox,
                       ISyncHttpClient syncClient,
                       IDbContextFactory<AppDbContext> dbFactory,
                       ILastSyncVersionStore versionStore,
                       ISyncChangeApplier applier,
                       HairCarePlus.Client.Patient.Features.Progress.Services.Interfaces.IRestrictionService restrictionService,
                       ILogger<SyncService> logger)
    {
        _outbox = outbox;
        _syncClient = syncClient;
        _dbFactory = dbFactory;
        _versionStore = versionStore;
        _applier = applier;
        _restrictionService = restrictionService;
        _logger = logger;
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

        // Build restrictions (always include active list for now)
        var activeRestrictions = await _restrictionService.GetActiveRestrictionsAsync(cancellationToken);
        _logger.LogInformation("Patient Sync: active restrictions fetched = {Count}", activeRestrictions.Count);

        var restrictionDtos = activeRestrictions.Select(r => new HairCarePlus.Shared.Communication.RestrictionDto
        {
            Id = Guid.Empty, // сервер назначит при первой вставке
            PatientId = _patientId,
            Type = MapRestrictionType(r.IconType),
            IconType = r.IconType,
            // фиксируемся на полуночь, чтобы ключ был детерминирован в течение дня
            StartUtc = DateTime.UtcNow.Date.AddDays(-(r.TotalDays - r.DaysRemaining)),
            EndUtc = DateTime.UtcNow.Date.AddDays(r.DaysRemaining - 1),
            IsActive = r.DaysRemaining > 0
        }).ToList();

        var req = new BatchSyncRequestDto
        {
            ClientId = $"patient-{_patientId}",
            LastSyncVersion = lastVersion,
            PhotoReportHeaders = headers,
            PhotoReports = photoReportsToSend.Count > 0 ? photoReportsToSend : null,
            PhotoComments = photoCommentsToSend.Count > 0 ? photoCommentsToSend : null,
            AckIds = _pendingAckIds.Count > 0 ? _pendingAckIds : null,
            Restrictions = restrictionDtos.Count > 0 ? restrictionDtos : null
        };

        _logger.LogInformation(
            "Patient Sync: sending request. Reports={Reports}, Comments={Comments}, Restrictions={Restrictions}, AckIds={Ack}",
            photoReportsToSend.Count, photoCommentsToSend.Count, restrictionDtos.Count, _pendingAckIds.Count);

        // 3. Call API ----------------------------------------------------
        var response = await _syncClient.PushAsync(req, cancellationToken);

        if (response == null)
        {
            _logger.LogWarning("Patient Sync: response is null (network error)");
            return;
        }

        _logger.LogInformation(
            "Patient Sync: received response. PhotoReports={Reports}, Comments={Comments}, Restrictions={Restrictions}, Packets={Packets}",
            response.PhotoReports?.Count ?? 0,
            response.PhotoComments?.Count ?? 0,
            response.Restrictions?.Count ?? 0,
            response.Packets?.Count ?? 0);

        // 3.1 Process incoming packets ----------------------------------
        if (response.Packets != null)
        {
            await using var db2 = _dbFactory.CreateDbContext();
            foreach (var p in response.Packets)
            {
                switch (p.EntityType)
                {
                    case "PhotoReport":
                        try
                        {
                            var dto = JsonSerializer.Deserialize<HairCarePlus.Shared.Communication.PhotoReportDto>(p.PayloadJson);
                            if (dto != null && await db2.PhotoReports.FirstOrDefaultAsync(r => r.Id == dto.Id.ToString()) == null)
                            {
                                var entity = new PhotoReportEntity
                                {
                                    Id = dto.Id.ToString(),
                                    ImageUrl = dto.ImageUrl,
                                    CaptureDate = dto.Date,
                                    DoctorComment = dto.Notes
                                };
                                await db2.PhotoReports.AddAsync(entity, cancellationToken);
                                _pendingAckIds.Add(p.Id);
                            }
                        }
                        catch { /* ignore */ }
                        break;
                }
            }
            await db2.SaveChangesAsync(cancellationToken);
        }

        // 4. Apply server changes locally --------------------------------
        await _applier.ApplyAsync(response);

        // 5. Update Outbox status ---------------------------------------
        if (pendingItems.Count > 0)
            await _outbox.UpdateStatusAsync(pendingItems.Select(i => i.Id), SyncStatus.Acked);

        // 6. Save new sync version --------------------------------------
        await _versionStore.SetAsync(response.NewSyncVersion);

        // 7. Clear ack list after successful round
        _pendingAckIds.Clear();

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
                    PatientId = _patientId,
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

    private static HairCarePlus.Shared.Communication.RestrictionType MapRestrictionType(HairCarePlus.Shared.Domain.Restrictions.RestrictionIconType iconType)
        => iconType switch
        {
            HairCarePlus.Shared.Domain.Restrictions.RestrictionIconType.NoAlcohol => HairCarePlus.Shared.Communication.RestrictionType.Alcohol,
            HairCarePlus.Shared.Domain.Restrictions.RestrictionIconType.NoSporting or
            HairCarePlus.Shared.Domain.Restrictions.RestrictionIconType.NoSweating or
            HairCarePlus.Shared.Domain.Restrictions.RestrictionIconType.NoSwimming => HairCarePlus.Shared.Communication.RestrictionType.Sport,
            HairCarePlus.Shared.Domain.Restrictions.RestrictionIconType.NoSun => HairCarePlus.Shared.Communication.RestrictionType.Sauna,
            _ => HairCarePlus.Shared.Communication.RestrictionType.Food
        };
} 