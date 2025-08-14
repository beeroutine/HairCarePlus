using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Client.Clinic.Features.Sync.Domain.Entities;
using HairCarePlus.Shared.Communication;
using HairCarePlus.Client.Clinic.Features.Sync.Infrastructure;
using HairCarePlus.Client.Clinic.Infrastructure.Storage;
using HairCarePlus.Shared.Communication.Sync;
using Microsoft.EntityFrameworkCore;
using HairCarePlus.Client.Clinic.Features.Chat.Domain;
using HairCarePlus.Client.Clinic.Infrastructure.Features.Chat.Repositories;
using HairCarePlus.Client.Clinic.Infrastructure.FileCache;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Messaging;
using HairCarePlus.Client.Clinic.Infrastructure.Features.Progress.Messages;

namespace HairCarePlus.Client.Clinic.Features.Sync.Application;

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
    private readonly IChatMessageRepository _chatRepo;
    private readonly IFileCacheService _fileCache;
    private readonly IAckStore _ackStore;
    private readonly ILogger<SyncService> _logger;
    private readonly IMessenger _messenger;

    private readonly List<Guid> _pendingAckIds = new();
    private static readonly Guid _deviceId = Guid.NewGuid();

    public SyncService(HairCarePlus.Shared.Communication.IOutboxRepository outbox,
                       ISyncHttpClient syncClient,
                       IDbContextFactory<AppDbContext> dbFactory,
                       ILastSyncVersionStore versionStore,
                       ISyncChangeApplier applier,
                       IChatMessageRepository chatRepo,
                       IFileCacheService fileCache,
                       IAckStore ackStore,
                       ILogger<SyncService> logger,
                       IMessenger messenger,
                       HairCarePlus.Client.Clinic.Infrastructure.Network.Events.IEventsSubscription events)
    {
        _outbox = outbox;
        _syncClient = syncClient;
        _dbFactory = dbFactory;
        _versionStore = versionStore;
        _applier = applier;
        _chatRepo = chatRepo;
        _fileCache = fileCache;
        _ackStore = ackStore;
        _logger = logger;
        _messenger = messenger;
        // Auto-sync on new PhotoReportSet events for near-instant UI
        events.PhotoReportSetAdded += async (_, __) => { try { await SynchronizeAsync(CancellationToken.None); } catch { } };
    }

    // Prevent concurrent syncs to avoid DB race conditions
    private static readonly System.Threading.SemaphoreSlim _syncLock = new(1, 1);
    public async Task SynchronizeAsync(CancellationToken cancellationToken)
    {
        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            // 1. Gather pending local changes
            var pendingItems = await _outbox.GetPendingItemsAsync();

            await using var db = _dbFactory.CreateDbContext();

            var photoReportsToSend = new List<HairCarePlus.Shared.Communication.PhotoReportDto>();
            var photoCommentsToSend = new List<HairCarePlus.Shared.Communication.PhotoCommentDto>();
            var chatMessagesToSend = new List<HairCarePlus.Shared.Communication.ChatMessageDto>();

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
                    case "ChatMessage":
                        try
                        {
                            var chatDto = JsonSerializer.Deserialize<HairCarePlus.Shared.Communication.ChatMessageDto>(item.PayloadJson);
                            if (chatDto != null)
                                chatMessagesToSend.Add(chatDto);
                        }
                        catch { }
                        break;
                }
            }

            // prepare headers (clinic should not reconstruct history from server; headers are optional)
            var reportHeaders = new List<HairCarePlus.Shared.Communication.EntityHeaderDto>();

            var lastVersion = await _versionStore.GetAsync();

            // Merge persisted ACK ids (survive restarts) into in-memory list
            try
            {
                var persistedAcks = await _ackStore.LoadAsync();
                foreach (var id in persistedAcks)
                {
                    if (!_pendingAckIds.Contains(id))
                        _pendingAckIds.Add(id);
                }
            }
            catch { }

            // Snapshot ACKs included in the first request
            var firstAckBatch = _pendingAckIds.ToList();

            var request = new BatchSyncRequestDto
            {
                DeviceId = _deviceId,
                ClientId = "clinic-app", // TODO: unique device id
                LastSyncVersion = lastVersion,
                 PhotoReportHeaders = reportHeaders,
                PhotoReports = photoReportsToSend.Count > 0 ? photoReportsToSend : null,
                PhotoComments = photoCommentsToSend,
                ChatMessages = chatMessagesToSend,
                AckIds = firstAckBatch.Count > 0 ? firstAckBatch : null
            };

            var response = await _syncClient.PushAsync(request, cancellationToken);
            if (response == null) return; // network error

            // First request succeeded: remove sent ACKs from persistent store and in-memory list
            if (firstAckBatch.Count > 0)
            {
                try { await _ackStore.RemoveAsync(firstAckBatch); } catch { }
                foreach (var id in firstAckBatch) _pendingAckIds.Remove(id);
            }

            // 2. Process incoming packets
            if (response.Packets != null)
            {
                var notifyPatientIds = new HashSet<string>();
                await using var db2 = _dbFactory.CreateDbContext();
                var handledRestrictionIds = new HashSet<string>();
                var handledPhotoIds = new HashSet<string>();
                foreach (var packet in response.Packets)
                {
                    switch (packet.EntityType)
                    {
                        case "PhotoReport":
                            try
                            {
                                var dto = JsonSerializer.Deserialize<HairCarePlus.Shared.Communication.PhotoReportDto>(packet.PayloadJson);
                                if (dto != null)
                                {
                                    var id = dto.Id.ToString();
                                    // skip duplicates within the same batch
                                    if (!handledPhotoIds.Add(id))
                                        break;

                                    // check ChangeTracker first to avoid double AddAsync in this context
                                    var tracked = db2.ChangeTracker.Entries<Domain.Entities.PhotoReportEntity>()
                                                     .FirstOrDefault(e => e.Entity.Id == id)?.Entity;

                                    var entity = tracked ?? await db2.PhotoReports.FirstOrDefaultAsync(r => r.Id == id);

                                    if (entity == null)
                                    {
                                        entity = new Domain.Entities.PhotoReportEntity
                                        {
                                            Id = id,
                                            PatientId = dto.PatientId.ToString(),
                                            SetId = dto.SetId?.ToString(),
                                            ImageUrl = dto.ImageUrl,
                                            Date = dto.Date,
                                            DoctorComment = dto.Notes ?? string.Empty,
                                            Type = dto.Type
                                        };
                                        try
                                        {
                                            if (!string.IsNullOrWhiteSpace(dto.ImageUrl) &&
                                                (dto.ImageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                                 dto.ImageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                                            {
                                                var path = await _fileCache.GetLocalPathAsync(dto.ImageUrl, cancellationToken);
                                                if (path != null)
                                                    entity.LocalPath = path;
                                            }
                                        }
                                        catch { /* download failure is non-fatal */ }
                                        await db2.PhotoReports.AddAsync(entity, cancellationToken);
                                    }

                                    // Track patient for UI notification
                                    notifyPatientIds.Add(dto.PatientId.ToString());

                                    // ACK only if local file is confirmed cached
                                    bool HasLocalFile(string? p)
                                    {
                                        if (string.IsNullOrWhiteSpace(p)) return false;
                                        var resolved = p;
                                        try
                                        {
                                            if (!System.IO.Path.IsPathRooted(resolved))
                                                resolved = System.IO.Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, resolved);
                                        }
                                        catch { }
                                        return System.IO.File.Exists(resolved);
                                    }
                                    if (HasLocalFile(entity.LocalPath))
                                    {
                                        // ACK DeliveryQueue packet by its Id (not the inner entity id)
                                        _pendingAckIds.Add(packet.Id);
                                        try { await _ackStore.AddAsync(packet.Id); } catch { }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to process PhotoReport packet {PacketId}", packet.Id);
                            }
                            break;
                        case nameof(HairCarePlus.Shared.Communication.PhotoReportSetDto):
                            try
                            {
                                var set = JsonSerializer.Deserialize<HairCarePlus.Shared.Communication.PhotoReportSetDto>(packet.PayloadJson);
                                if (set != null)
                                {
                                    bool allCached = true;
                                    foreach (var it in set.Items)
                                    {
                                        var id = Guid.NewGuid().ToString();
                                        var entity = new Domain.Entities.PhotoReportEntity
                                        {
                                            Id = id,
                                            PatientId = set.PatientId.ToString(),
                                            SetId = set.Id.ToString(),
                                            ImageUrl = it.ImageUrl,
                                            Date = set.Date,
                                            DoctorComment = set.Notes ?? string.Empty,
                                            Type = it.Type
                                        };
                                        try
                                        {
                                            if (!string.IsNullOrWhiteSpace(it.ImageUrl) &&
                                                (it.ImageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                                 it.ImageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                                            {
                                                var path = await _fileCache.GetLocalPathAsync(it.ImageUrl, cancellationToken);
                                                if (path != null)
                                                    entity.LocalPath = path;
                                            }
                                        }
                                        catch { }
                                        await db2.PhotoReports.AddAsync(entity, cancellationToken);

                                        bool HasLocalFile(string? p)
                                        {
                                            if (string.IsNullOrWhiteSpace(p)) return false;
                                            var resolved = p;
                                            try
                                            {
                                                if (!System.IO.Path.IsPathRooted(resolved))
                                                    resolved = System.IO.Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, resolved);
                                            }
                                            catch { }
                                            return System.IO.File.Exists(resolved);
                                        }
                                        allCached = allCached && HasLocalFile(entity.LocalPath);
                                    }
                                    // Track patient for UI notification
                                    notifyPatientIds.Add(set.PatientId.ToString());
                                    // ACK только если все 3 файла набора закешированы локально
                                    if (allCached)
                                    {
                                        // Для DeliveryQueue ACK нужен Id пакета, а не внутренний Set.Id
                                        _pendingAckIds.Add(packet.Id);
                                        try { await _ackStore.AddAsync(packet.Id); } catch { }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to process PhotoReportSet packet {PacketId}", packet.Id);
                            }
                            break;
                        case "Restriction":
                            try
                            {
                                var dto = JsonSerializer.Deserialize<HairCarePlus.Shared.Communication.RestrictionDto>(packet.PayloadJson);
                                if (dto != null)
                                {
                                    var id = dto.Id.ToString();
                                    // skip duplicate within same batch
                                    if (!handledRestrictionIds.Add(id))
                                        break;

                                    // Check ChangeTracker first
                                    var tracked = db2.ChangeTracker.Entries<Domain.Entities.RestrictionEntity>()
                                                             .FirstOrDefault(e => e.Entity.Id == id)?.Entity;

                                    var entity = tracked ?? await db2.Restrictions.FirstOrDefaultAsync(r => r.Id == id);
                                    if (entity == null)
                                    {
                                        entity = new Domain.Entities.RestrictionEntity
                                        {
                                            Id = id,
                                            PatientId = dto.PatientId.ToString(),
                                            Type = (int)dto.IconType,
                                            StartUtc = dto.StartUtc,
                                            EndUtc = dto.EndUtc,
                                            IsActive = dto.IsActive
                                        };
                                        await db2.Restrictions.AddAsync(entity, cancellationToken);
                                    }
                                    else
                                    {
                                        entity.IsActive = dto.IsActive;
                                    }
                                    // ACK by DeliveryQueue packet Id
                                    _pendingAckIds.Add(packet.Id);
                                    try { await _ackStore.AddAsync(packet.Id); } catch { }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to process Restriction packet {PacketId}", packet.Id);
                            }
                            break;
                        case "ChatMessage":
                            try
                            {
                                var dto = JsonSerializer.Deserialize<HairCarePlus.Shared.Communication.ChatMessageDto>(packet.PayloadJson);
                                if (dto != null)
                                {
                                    var entity = new HairCarePlus.Client.Clinic.Features.Chat.Models.ChatMessage
                                    {
                                        ConversationId = dto.ConversationId,
                                        SenderId = dto.SenderId,
                                        Content = dto.Content,
                                        SentAt = dto.SentAt,
                                        SyncStatus = HairCarePlus.Client.Clinic.Features.Chat.Models.SyncStatus.Synced
                                    };
                                    await _chatRepo.AddAsync(entity);
                                    // ACK DeliveryQueue packet by its Id
                                    _pendingAckIds.Add(packet.Id);
                                    try { await _ackStore.AddAsync(packet.Id); } catch { }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to process ChatMessage packet {PacketId}", packet.Id);
                            }
                            break;
                    }
                }
                await db2.SaveChangesAsync(cancellationToken);
                // Notify UI to reload feed for affected patients (after DB commit)
                foreach (var pid in notifyPatientIds)
                {
                    try { _messenger.Send(new PhotoReportsChangedMessage(pid)); } catch { }
                }
            }

            // 3. Apply server changes via applier
            await _applier.ApplyAsync(response);

            // 4. Update outbox statuses
            if (pendingItems.Count > 0)
                await _outbox.UpdateStatusAsync(pendingItems.Select(i => i.Id), OutboxStatus.Acked);

            // 5. Save new version
            await _versionStore.SetAsync(response.NewSyncVersion);

            // 6. Immediately send ACK-only request for new ACKs captured during this cycle
            var newAckBatch = _pendingAckIds.ToList();
            if (newAckBatch.Count > 0)
            {
                try
                {
                    var ackReq = new BatchSyncRequestDto
                    {
                        DeviceId = _deviceId,
                        ClientId = "clinic-app",
                        LastSyncVersion = response.NewSyncVersion,
                        AckIds = newAckBatch
                    };
                    var ackResp = await _syncClient.PushAsync(ackReq, cancellationToken);
                    if (ackResp != null)
                    {
                        await _ackStore.RemoveAsync(newAckBatch);
                        foreach (var id in newAckBatch) _pendingAckIds.Remove(id);
                    }
                }
                catch { }
            }

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
        finally
        {
            _syncLock.Release();
        }
    }
} 