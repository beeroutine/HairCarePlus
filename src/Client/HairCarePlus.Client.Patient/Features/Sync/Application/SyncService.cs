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
using HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;

namespace HairCarePlus.Client.Patient.Features.Sync.Application
{
    public interface ISyncService
    {
        Task SynchronizeAsync(CancellationToken cancellationToken);
    }

    public class SyncService : ISyncService
    {
        private readonly HairCarePlus.Shared.Communication.IOutboxRepository _outbox;
        private readonly ISyncHttpClient _syncClient;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILastSyncVersionStore _versionStore;
        private readonly ISyncChangeApplier _applier;
        private readonly ILogger<SyncService> _logger;
        private readonly IRestrictionService _restrictionService;
        private readonly HairCarePlus.Client.Patient.Infrastructure.Media.IUploadService _uploadService;
        private readonly List<Guid> _pendingAckIds = new();
        private readonly IChatRepository _chatRepository;
        private static readonly Guid _deviceId = Guid.NewGuid();
        private static readonly Guid _patientId = Guid.Parse("35883846-63ee-4cf8-b930-25e61ec1f540");

        public SyncService(HairCarePlus.Shared.Communication.IOutboxRepository outbox,
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
            var photoReportSetsToSend = new List<PhotoReportSetDto>();
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

                            // 1) Если уже HTTP-ссылка – просто берём как есть
                            if (isHttp)
                            {
                                photoReportsToSend.Add(reportDto);
                            }
                            else if (!string.IsNullOrEmpty(reportDto.ImageUrl) && File.Exists(reportDto.ImageUrl))
                            {
                                // 2) Локальный путь существует – загружаем и меняем на URL
                                await TryUploadAndQueueAsync(reportDto, item);
                            }
                            else if (!string.IsNullOrEmpty(reportDto.LocalPath) && File.Exists(reportDto.LocalPath))
                            {
                                // 3) ImageUrl устарел, но LocalPath содержит актуальный путь – используем его
                                _logger.LogInformation("Using LocalPath fallback for PhotoReport {Id}", reportDto.Id);
                                reportDto.ImageUrl = reportDto.LocalPath;
                                await TryUploadAndQueueAsync(reportDto, item);
                            }
                            else
                            {
                                _logger.LogWarning("PhotoReport file not found. Id={Id} ImageUrl={Url} LocalPath={Local}", reportDto.Id, reportDto.ImageUrl, reportDto.LocalPath);
                            }

                            async Task TryUploadAndQueueAsync(PhotoReportDto dto, OutboxItemDto outboxItem)
                            {
                                try
                                {
                                    var fileName = Path.GetFileName(dto.ImageUrl);
                                    var newUrl = await _uploadService.UploadFileAsync(dto.ImageUrl, fileName);
                                    if (!string.IsNullOrEmpty(newUrl))
                                    {
                                        dto.ImageUrl = newUrl;
                                        outboxItem.PayloadJson = JsonSerializer.Serialize(dto);
                                        outboxItem.ModifiedAtUtc = DateTime.UtcNow;
                                        // Persist updated payload via repository (future optimisation)
                                    }

                                    photoReportsToSend.Add(dto);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Failed to upload photo for PhotoReport {Id}", dto.Id);
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Failed to deserialize PhotoReport from outbox item {Id}", item.Id);
                        }
                        break;
                    case nameof(PhotoReportSetDto):
                        try
                        {
                            var setDto = JsonSerializer.Deserialize<PhotoReportSetDto>(item.PayloadJson);
                            if (setDto == null) break;

                            // Ensure exactly three items and try to guarantee HTTP urls by uploading any local files
                            if (setDto.Items.Count == 3)
                            {
                                for (int i = 0; i < setDto.Items.Count; i++)
                                {
                                    var it = setDto.Items[i];
                                    var hasHttp = !string.IsNullOrEmpty(it.ImageUrl) && it.ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase);
                                    string? candidatePath = !string.IsNullOrEmpty(it.LocalPath) && File.Exists(it.LocalPath)
                                        ? it.LocalPath
                                        : (!string.IsNullOrEmpty(it.ImageUrl) && File.Exists(it.ImageUrl) ? it.ImageUrl : null);

                                    if (!hasHttp && candidatePath != null)
                                    {
                                        try
                                        {
                                            if (string.IsNullOrEmpty(candidatePath))
                                                break;
                                            var fileName = System.IO.Path.GetFileName(candidatePath);
                                            if (string.IsNullOrEmpty(fileName))
                                                break;
                                            var newUrl = await _uploadService.UploadFileAsync(candidatePath, fileName);
                                            if (!string.IsNullOrEmpty(newUrl))
                                            {
                                                it.ImageUrl = newUrl;
                                                it.LocalPath = candidatePath; // keep local for offline
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogWarning(ex, "Failed to upload item {Index} for PhotoReportSet {SetId}", i, setDto.Id);
                                        }
                                    }
                                }

                                // Only send when ALL three items have HTTP urls to avoid broken images in Clinic
                                var httpReady = setDto.Items.All(i => !string.IsNullOrWhiteSpace(i.ImageUrl) && i.ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase));
                                if (httpReady)
                                {
                                    photoReportSetsToSend.Add(setDto);
                                }
                                else
                                {
                                    // Keep in outbox for next sync attempt (uploads may succeed later)
                                    _logger.LogInformation("PhotoReportSet {Id} deferred: waiting for HTTP URLs for all items", setDto.Id);
                                    // reflect any uploads that did succeed back into payload for next run
                                    item.PayloadJson = JsonSerializer.Serialize(setDto);
                                    item.ModifiedAtUtc = DateTime.UtcNow;
                                }
                            }
                            else
                            {
                                _logger.LogWarning("PhotoReportSet {Id} has {Count} items; expected 3. Sending anyway.", setDto.Id, setDto.Items.Count);
                                photoReportSetsToSend.Add(setDto);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to process PhotoReportSet outbox item {Id}", item.Id);
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
                            {
                                // If message has a local attachment – upload and swap to HTTP URL (Instagram-like local-first)
                                bool needsUpload = !string.IsNullOrEmpty(chatDto.LocalAttachmentPath) &&
                                                   File.Exists(chatDto.LocalAttachmentPath) &&
                                                   string.IsNullOrEmpty(chatDto.AttachmentUrl);
                                if (needsUpload)
                                {
                                    try
                                    {
                                        var fileName = Path.GetFileName(chatDto.LocalAttachmentPath);
                                        var newUrl = await _uploadService.UploadFileAsync(chatDto.LocalAttachmentPath, fileName);
                                        if (!string.IsNullOrEmpty(newUrl))
                                        {
                                            chatDto.AttachmentUrl = newUrl;
                                            // keep LocalAttachmentPath for offline preview; network peers will use AttachmentUrl
                                            // reflect change back into outbox payload
                                            item.PayloadJson = JsonSerializer.Serialize(chatDto);
                                            item.ModifiedAtUtc = DateTime.UtcNow;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Failed to upload chat attachment for localId={LocalId}", item.LocalEntityId);
                                    }
                                }

                                chatMessagesToSend.Add(chatDto);
                            }
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
                DeviceId = _deviceId,
                PatientId = _patientId,
                ClientId = $"patient-{_patientId}",
                LastSyncVersion = lastVersion,
                PhotoReportHeaders = headers,
                PhotoReports = photoReportsToSend.Count > 0 ? photoReportsToSend : null,
                PhotoReportSets = photoReportSetsToSend.Count > 0 ? photoReportSetsToSend : null,
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

            if (response.Packets != null && response.Packets.Count > 0)
            {
                await using var db2 = await _dbFactory.CreateDbContextAsync(cancellationToken);
                foreach (var p in response.Packets)
                {
                    switch (p.EntityType)
                    {
                        case "PhotoReport":
                            try
                            {
                                var reportDto = JsonSerializer.Deserialize<PhotoReportDto>(p.PayloadJson);
                                if (reportDto != null)
                                {
                                    var entity = new PhotoReportEntity
                                    {
                                        Id = reportDto.Id.ToString(),
                                        PatientId = reportDto.PatientId.ToString(),
                                        SetId = reportDto.SetId?.ToString(),
                                        ImageUrl = reportDto.ImageUrl,
                                        CaptureDate = reportDto.Date,
                                        DoctorComment = reportDto.Notes,
                                        Zone = MapZone(reportDto.Type),
                                    };
                                    db2.PhotoReports.Add(entity);
                                    _pendingAckIds.Add(p.Id);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to process PhotoReport packet {PacketId}", p.Id);
                            }
                            break;
                        case "ChatMessage":
                            try
                            {
                                var dto = JsonSerializer.Deserialize<ChatMessageDto>(p.PayloadJson);
                                if (dto != null)
                                {
                                    // Persist incoming chat locally for offline access
                                    await _chatRepository.SaveMessageAsync(dto, cancellationToken);
                                    _pendingAckIds.Add(p.Id);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to process ChatMessage packet {PacketId}", p.Id);
                            }
                            break;
                    }
                }
                await db2.SaveChangesAsync(cancellationToken);
            }

            await _applier.ApplyAsync(response);

            // Частичный ACK: подтверждаем только те элементы, которые реально ушли в запрос
            var ackIds = new List<int>();
            foreach (var item in pendingItems)
            {
                switch (item.EntityType)
                {
                    case "PhotoReport":
                        if (photoReportsToSend.Any(r => r.Id.ToString() == item.LocalEntityId || r.Id != Guid.Empty && item.Payload.Contains(r.Id.ToString(), StringComparison.OrdinalIgnoreCase)))
                            ackIds.Add(item.Id);
                        break;
                    case nameof(PhotoReportSetDto):
                        if (photoReportSetsToSend.Any(s => s.Id.ToString() == item.LocalEntityId || item.Payload.Contains(s.Id.ToString(), StringComparison.OrdinalIgnoreCase)))
                            ackIds.Add(item.Id);
                        break;
                    case "PhotoComment":
                        if (photoCommentsToSend.Any()) ackIds.Add(item.Id);
                        break;
                    case "ChatMessage":
                        if (chatMessagesToSend.Any()) ackIds.Add(item.Id);
                        break;
                }
            }
            if (ackIds.Count > 0)
                await _outbox.UpdateStatusAsync(ackIds, HairCarePlus.Shared.Communication.OutboxStatus.Acked);

            foreach (var item in pendingItems.Where(i => i.EntityType == "ChatMessage"))
            {
                if (int.TryParse(item.LocalEntityId, out var localId))
                {
                    await _chatRepository.UpdateSyncStatusAsync(localId, HairCarePlus.Shared.Communication.SyncStatus.Synced, cancellationToken: cancellationToken);
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

                    await _outbox.AddAsync(new OutboxItemDto
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

        private static HairCarePlus.Client.Patient.Features.Progress.Domain.Entities.PhotoZone MapZone(PhotoType type)
            => type switch
            {
                PhotoType.FrontView => HairCarePlus.Client.Patient.Features.Progress.Domain.Entities.PhotoZone.Front,
                PhotoType.TopView => HairCarePlus.Client.Patient.Features.Progress.Domain.Entities.PhotoZone.Top,
                PhotoType.BackView => HairCarePlus.Client.Patient.Features.Progress.Domain.Entities.PhotoZone.Back,
                PhotoType.LeftSideView => HairCarePlus.Client.Patient.Features.Progress.Domain.Entities.PhotoZone.Front,
                PhotoType.RightSideView => HairCarePlus.Client.Patient.Features.Progress.Domain.Entities.PhotoZone.Front,
                _ => HairCarePlus.Client.Patient.Features.Progress.Domain.Entities.PhotoZone.Front
            };
    }
} 