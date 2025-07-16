using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Chat.Domain.Repositories;
using HairCarePlus.Client.Patient.Features.Progress.Services.Interfaces;
using HairCarePlus.Client.Patient.Features.Sync.Domain.Entities;
using HairCarePlus.Client.Patient.Features.Sync.Infrastructure;
using HairCarePlus.Client.Patient.Infrastructure.Storage;
using HairCarePlus.Shared.Communication;
using HairCarePlus.Shared.Communication.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Client.Patient.Features.Sync.Application
{
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
        private readonly IRestrictionService _restrictionService;
        private readonly HairCarePlus.Client.Patient.Infrastructure.Media.IUploadService _uploadService;
        private readonly List<Guid> _pendingAckIds = new();
        private readonly IChatRepository _chatRepository;
        private static readonly Guid _patientId = Guid.Parse("8f8c7e0b-1234-4e78-a8cc-ff0011223344");

        public SyncService(IOutboxRepository outbox,
                           ISyncHttpClient syncClient,
                           IDbContextFactory<AppDbContext> dbFactory,
                           ILastSyncVersionStore versionStore,
                           ISyncChangeApplier applier,
                           IRestrictionService restrictionService,
                           ILogger<SyncService> logger,
                           IChatRepository chatRepository,
                           HairCarePlus.Client.Patient.Infrastructure.Media.IUploadService uploadService)
        {
            _outbox = outbox;
            _syncClient = syncClient;
            _dbFactory = dbFactory;
            _versionStore = versionStore;
            _applier = applier;
            _restrictionService = restrictionService;
            _logger = logger;
            _chatRepository = chatRepository;
            _uploadService = uploadService;
        }

        public async Task SynchronizeAsync(CancellationToken cancellationToken)
        {
            var pendingItems = await _outbox.GetPendingItemsAsync();

            await using var db = _dbFactory.CreateDbContext();

            var photoReportsToSend = new List<PhotoReportDto>();
            var photoCommentsToSend = new List<PhotoCommentDto>();
            var chatMessagesToSend = new List<ChatMessageDto>();

            foreach (var item in pendingItems)
            {
                switch (item.EntityType)
                {
                    case "PhotoReport":
                        try
                        {
                            var reportDto = JsonSerializer.Deserialize<PhotoReportDto>(item.PayloadJson);
                            if (reportDto == null) continue;

                            bool isHttp = !string.IsNullOrEmpty(reportDto.ImageUrl) &&
                                          reportDto.ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase);

                            if (isHttp)
                            {
                                photoReportsToSend.Add(reportDto);
                            }
                            else if (!string.IsNullOrEmpty(reportDto.ImageUrl)) // It's a local path
                            {
                                if (File.Exists(reportDto.ImageUrl))
                                {
                                    try
                                    {
                                        var fileName = Path.GetFileName(reportDto.ImageUrl);
                                        var newUrl = await _uploadService.UploadFileAsync(reportDto.ImageUrl, fileName);
                                        if (!string.IsNullOrEmpty(newUrl))
                                        {
                                            reportDto.ImageUrl = newUrl;
                                            item.PayloadJson = JsonSerializer.Serialize(reportDto);
                                            item.ModifiedAtUtc = DateTime.UtcNow;
                                            await using (var ctx = _dbFactory.CreateDbContext())
                                            {
                                                ctx.OutboxItems.Attach(item);
                                                ctx.Entry(item).Property(o => o.PayloadJson).IsModified = true;
                                                ctx.Entry(item).Property(o => o.ModifiedAtUtc).IsModified = true;
                                                await ctx.SaveChangesAsync();
                                            }
                                            photoReportsToSend.Add(reportDto);
                                        }
                                        else
                                        {
                                            _logger.LogWarning("Upload of legacy photo {Path} returned empty URL, skipping.", reportDto.ImageUrl);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Failed to upload legacy photo path {Path}", reportDto.ImageUrl);
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("Legacy photo path not found, skipping: {Path}", reportDto.ImageUrl);
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Failed to deserialize PhotoReport from outbox item {Id}", item.Id);
                        }
                        break;
                    case "PhotoComment":
                        try
                        {
                            var commentDto = JsonSerializer.Deserialize<PhotoCommentDto>(item.PayloadJson);
                            if (commentDto != null)
                                photoCommentsToSend.Add(commentDto);
                        }
                        catch { }
                        break;
                    case "ChatMessage":
                        try
                        {
                            var chatDto = JsonSerializer.Deserialize<ChatMessageDto>(item.PayloadJson);
                            if (chatDto != null)
                                chatMessagesToSend.Add(chatDto);
                        }
                        catch { }
                        break;
                }
            }

            var headers = await db.PhotoReports
                                   .Select(r => new EntityHeaderDto
                                   {
                                       Id = Guid.Parse(r.Id),
                                       ModifiedAtUtc = r.CaptureDate.ToUniversalTime()
                                   })
                                   .ToListAsync(cancellationToken);

            var lastVersion = await _versionStore.GetAsync();

            var activeRestrictions = await _restrictionService.GetActiveRestrictionsAsync(cancellationToken);
            _logger.LogInformation("Patient Sync: active restrictions fetched = {Count}", activeRestrictions.Count);

            var restrictionDtos = activeRestrictions.Select(r => new RestrictionDto
            {
                Id = Guid.Empty,
                PatientId = _patientId,
                Type = MapRestrictionType(r.IconType),
                IconType = r.IconType,
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
                ChatMessages = chatMessagesToSend.Count > 0 ? chatMessagesToSend : null,
                AckIds = _pendingAckIds.Count > 0 ? _pendingAckIds : null,
                Restrictions = restrictionDtos.Count > 0 ? restrictionDtos : null
            };

            _logger.LogInformation(
                "Patient Sync: sending request. Reports={Reports}, Comments={Comments}, Chat={Chat}, Restrictions={Restrictions}, AckIds={Ack}",
                photoReportsToSend.Count, photoCommentsToSend.Count, chatMessagesToSend.Count, restrictionDtos.Count, _pendingAckIds.Count);

            var response = await _syncClient.PushAsync(req, cancellationToken);

            if (response == null)
            {
                _logger.LogWarning("Patient Sync: response is null (network error)");
                return;
            }

            _logger.LogInformation(
                "Patient Sync: received response. PhotoReports={Reports}, Comments={Comments}, Chat={Chat}, Restrictions={Restrictions}, Packets={Packets}",
                response.PhotoReports?.Count ?? 0,
                response.PhotoComments?.Count ?? 0,
                response.ChatMessages?.Count ?? 0,
                response.Restrictions?.Count ?? 0,
                response.Packets?.Count ?? 0);

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
                                var dto = JsonSerializer.Deserialize<PhotoReportDto>(p.PayloadJson);
                                if (dto != null && await db2.PhotoReports.FirstOrDefaultAsync(r => r.Id == dto.Id.ToString(), cancellationToken) == null)
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

            await _applier.ApplyAsync(response);

            if (pendingItems.Count > 0)
                await _outbox.UpdateStatusAsync(pendingItems.Select(i => i.Id), SyncStatus.Acked);

            foreach (var item in pendingItems.Where(i => i.EntityType == "ChatMessage"))
            {
                if (int.TryParse(item.LocalEntityId, out var localId))
                {
                    await _chatRepository.UpdateSyncStatusAsync(localId, Chat.Domain.Entities.SyncStatus.Synced, cancellationToken: cancellationToken);
                }
            }

            await _versionStore.SetAsync(response.NewSyncVersion);

            _pendingAckIds.Clear();

            if (response.NeedPhotoReports != null && response.NeedPhotoReports.Count > 0)
            {
                var neededIds = response.NeedPhotoReports.Select(g => g.ToString()).ToHashSet();
                var neededReports = await db.PhotoReports.Include(r => r.Comments)
                                                         .Where(r => neededIds.Contains(r.Id))
                                                         .ToListAsync(cancellationToken);

                foreach (var rep in neededReports)
                {
                    var dto = new PhotoReportDto
                    {
                        Id = Guid.Parse(rep.Id),
                        PatientId = _patientId,
                        ImageUrl = rep.ImageUrl,
                        Date = rep.CaptureDate,
                        Notes = rep.DoctorComment ?? string.Empty,
                        Comments = rep.Comments.Select(c => new PhotoCommentDto
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

        private static Shared.Communication.RestrictionType MapRestrictionType(Shared.Domain.Restrictions.RestrictionIconType iconType)
            => iconType switch
            {
                Shared.Domain.Restrictions.RestrictionIconType.NoAlcohol => Shared.Communication.RestrictionType.Alcohol,
                Shared.Domain.Restrictions.RestrictionIconType.NoSporting or
                Shared.Domain.Restrictions.RestrictionIconType.NoSweating or
                Shared.Domain.Restrictions.RestrictionIconType.NoSwimming => Shared.Communication.RestrictionType.Sport,
                Shared.Domain.Restrictions.RestrictionIconType.NoSun => Shared.Communication.RestrictionType.Sauna,
                _ => Shared.Communication.RestrictionType.Food
            };
    }
} 