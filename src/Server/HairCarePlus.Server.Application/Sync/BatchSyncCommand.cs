using HairCarePlus.Shared.Communication.Sync;
using MediatR;
using HairCarePlus.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using HairCarePlus.Shared.Communication;
using HairCarePlus.Server.Domain.ValueObjects;
using HairCarePlus.Server.Infrastructure.Data.Repositories;
using System.Text.Json;
using System.Linq;
using Microsoft.Extensions.Logging;
using HairCarePlus.Shared.Communication.Events;

namespace HairCarePlus.Server.Application.Sync;

public record BatchSyncCommand(BatchSyncRequestDto Request) : IRequest<BatchSyncResponseDto>;

public class BatchSyncCommandHandler : IRequestHandler<BatchSyncCommand, BatchSyncResponseDto>
{
    private readonly AppDbContext _db;
    private readonly IDeliveryQueueRepository _dq;
    private readonly ILogger<BatchSyncCommandHandler> _logger;
    private readonly HairCarePlus.Server.Infrastructure.DeliveryOptions _opt;
    private readonly IEventsClient _events;

    public BatchSyncCommandHandler(AppDbContext db, IDeliveryQueueRepository dq, ILogger<BatchSyncCommandHandler> logger, Microsoft.Extensions.Options.IOptions<HairCarePlus.Server.Infrastructure.DeliveryOptions> opt, IEventsClient events)
    {
        _db = db;
        _dq = dq;
        _logger = logger;
        _opt = opt.Value;
        _events = events;
    }

    public async Task<BatchSyncResponseDto> Handle(BatchSyncCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;

        // Determine receiver & other side masks once
        byte receiverMask = req.ClientId.StartsWith("clinic", StringComparison.OrdinalIgnoreCase) ? (byte)1 : (byte)2;
        byte otherMask = receiverMask == 1 ? (byte)2 : (byte)1;

        // 0. Apply ACKs ------------------------------------------------
        if (req.AckIds != null && req.AckIds.Count > 0)
        {
            await _dq.AckAsync(req.AckIds, receiverMask);
        }

        // 1. APPLY INCOMING -----------------------
        var sinceUtc = DateTimeOffset.FromUnixTimeMilliseconds(req.LastSyncVersion).UtcDateTime;

        if (req.ChatMessages?.Count > 0)
        {
            foreach (var dto in req.ChatMessages)
            {
                Guid.TryParse(dto.ServerMessageId ?? string.Empty, out var messageGuid);
                var existing = messageGuid == Guid.Empty ? null : await _db.ChatMessages.FirstOrDefaultAsync(c => c.Id == messageGuid, cancellationToken);
                if (existing == null)
                {
                    await _db.ChatMessages.AddAsync(dto.ToEntity(), cancellationToken);
                }
                else if (dto.SentAt > existing.CreatedAt)
                {
                    existing.UpdateStatus((Domain.ValueObjects.MessageStatus)dto.Status);
                }
            }

            // enqueue packets for opposite side
            var chatPackets = req.ChatMessages.Select(dto => new HairCarePlus.Server.Domain.Entities.DeliveryQueue
            {
                EntityType = "ChatMessage",
                PayloadJson = JsonSerializer.Serialize(dto),
                PatientId = Guid.Empty, // Chat not bound to patient strictly; adjust if needed
                ReceiversMask = otherMask,
                DeliveredMask = 0,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(14)
            }).ToList();

            if (chatPackets.Count > 0)
                await _dq.AddRangeAsync(chatPackets);
        }

        if (req.PhotoComments?.Count > 0)
        {
            foreach (var dto in req.PhotoComments)
            {
                var existing = await _db.PhotoComments.FirstOrDefaultAsync(c => c.Id == dto.Id, cancellationToken);
                if (existing == null)
                {
                    await _db.PhotoComments.AddAsync(dto.ToEntity(), cancellationToken);
                }
                else if (dto.CreatedAtUtc > existing.CreatedAtUtc)
                {
                    existing.UpdateText(dto.Text);
                }
            }
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (req.CalendarTasks?.Count > 0)
        {
            foreach (var dto in req.CalendarTasks)
            {
                var existing = await _db.TreatmentSchedules.FirstOrDefaultAsync(c => c.Id == dto.Id, cancellationToken);
                if (existing == null)
                {
                    await _db.TreatmentSchedules.AddAsync(dto.ToEntity(), cancellationToken);
                }
                else if (dto.DueDateUtc > existing.StartDate) // Simplified update logic
                {
                    existing.UpdateSchedule(dto.DueDateUtc, null, existing.RecurrencePattern);
                }
            }
        }

        if (req.ProgressEntries?.Count > 0)
        {
            foreach (var dto in req.ProgressEntries)
            {
                var existing = await _db.ProgressEntries.FirstOrDefaultAsync(p => p.Id == dto.Id, cancellationToken);
                if (existing == null)
                    await _db.ProgressEntries.AddAsync(dto.ToEntity(), cancellationToken);
            }
        }

        if (req.Restrictions?.Count > 0)
        {
            // Ephemeral policy: do NOT persist restrictions on the server. Only relay via DeliveryQueue.
            _logger.LogInformation("BatchSync: received {Count} restrictions from {Client}", req.Restrictions.Count, req.ClientId);

            var restrictionPackets = req.Restrictions.Select(dto => new Domain.Entities.DeliveryQueue
            {
                EntityType = "Restriction",
                PayloadJson = JsonSerializer.Serialize(dto),
                PatientId = dto.PatientId,
                ReceiversMask = otherMask,
                DeliveredMask = 0,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(14)
            }).ToList();

            if (restrictionPackets.Count > 0)
                await _dq.AddRangeAsync(restrictionPackets);
            _logger.LogInformation("BatchSync: enqueued {Count} restriction packets for mask {Mask}", restrictionPackets.Count, otherMask);
        }

        // Handle incoming PhotoReports (patient -> server)
        // Ephemeral policy: do NOT persist PhotoReports in server DB. Only enqueue transient delivery packets.
        if (req.PhotoReports?.Count > 0)
        {
            var packetsToEnqueue = req.PhotoReports.Select(dto => new Domain.Entities.DeliveryQueue
            {
                EntityType = "PhotoReport",
                PayloadJson = JsonSerializer.Serialize(dto),
                PatientId = dto.PatientId,
                ReceiversMask = otherMask,
                DeliveredMask = 0,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(_opt.PhotoReportTtlDays)
            }).ToList();

            if (packetsToEnqueue.Count > 0)
                await _dq.AddRangeAsync(packetsToEnqueue);
        }

        // New: accept PhotoReportSet packets from patient and enqueue as a single atomic packet; also push realtime event
        if (req.PhotoReportSets != null && req.PhotoReportSets.Count > 0)
        {
            foreach (var set in req.PhotoReportSets)
            {
                try
                {
                    if (set == null || set.Items == null || set.Items.Count != 3) continue;
                    var packet = new Domain.Entities.DeliveryQueue
                    {
                        EntityType = nameof(HairCarePlus.Shared.Communication.PhotoReportSetDto),
                        PayloadJson = System.Text.Json.JsonSerializer.Serialize(set),
                        PatientId = set.PatientId,
                        ReceiversMask = otherMask,
                        DeliveredMask = 0,
                        ExpiresAtUtc = DateTime.UtcNow.AddDays(_opt.PhotoReportTtlDays)
                    };
                    await _dq.AddRangeAsync(new[] { packet });

                    // Push realtime event so Clinic triggers immediate sync
                    await _events.PhotoReportSetAdded(set.PatientId.ToString(), set);
                }
                catch { }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        // 2. COLLECT DELTA -------------------------
        var deltaChat = await _db.ChatMessages
            .Where(c => c.CreatedAt > sinceUtc)
            .Select(c => c.ToDto())
            .ToListAsync(cancellationToken);

        // Use base CreatedAt (DateTime) to avoid DateTimeOffset translation issues and rely on global soft-delete filter
        var deltaComments = await _db.PhotoComments
            .Where(c => c.CreatedAt > sinceUtc)
            .Select(c => c.ToDto())
            .ToListAsync(cancellationToken);

        var deltaCalendar = await _db.TreatmentSchedules
            .Where(c => c.CreatedAt > sinceUtc) // Simplified delta logic
            .Select(c => c.ToDto())
            .ToListAsync(cancellationToken);

        var deltaProgress = await _db.ProgressEntries
            .Where(p => p.CreatedAt > sinceUtc)
            .Select(p => p.ToDto())
            .ToListAsync(cancellationToken);
        
        // Ephemeral policy: do not return typed restrictions (deliver only via DeliveryQueue)
        List<RestrictionDto>? deltaRestrictions = null;

        // ---- PhotoReport differential sync -----------------------------
        // Ephemeral policy: server does not act as source of truth for PhotoReports.
        // Do not return any historical PhotoReports via typed list; rely solely on DeliveryQueue packets.
        List<PhotoReportDto> reportsToSend = new();
        List<Guid> needFromClient = new();

        // fetch packets for this receiver
        // If request carries PatientId (e.g., Patient app or Clinic scoped to a patient),
        // the repository will filter accordingly; otherwise falls back to all pending for receiver
        var pending = await _dq.GetPendingForReceiverAsync(req.PatientId, receiverMask);

        var packetsDto = pending.Select(p => new DeliveryPacketDto
        {
            Id = p.Id,
            EntityType = p.EntityType,
            PayloadJson = p.PayloadJson,
            BlobUrl = p.BlobUrl
        }).ToList();

        var resp = new BatchSyncResponseDto
        {
            NewSyncVersion = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ChatMessages = deltaChat,
            PhotoComments = deltaComments,
            CalendarTasks = deltaCalendar,
            PhotoReports = reportsToSend,
            NeedPhotoReports = needFromClient,
            ProgressEntries = deltaProgress,
            Restrictions = deltaRestrictions,
            Packets = packetsDto
        };

        return resp;
    }
} 