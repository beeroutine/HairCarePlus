using HairCarePlus.Shared.Communication.Sync;
using MediatR;
using HairCarePlus.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using HairCarePlus.Shared.Communication;
using HairCarePlus.Server.Domain.ValueObjects;

namespace HairCarePlus.Server.Application.Sync;

public record BatchSyncCommand(BatchSyncRequestDto Request) : IRequest<BatchSyncResponseDto>;

public class BatchSyncCommandHandler : IRequestHandler<BatchSyncCommand, BatchSyncResponseDto>
{
    private readonly AppDbContext _db;

    public BatchSyncCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<BatchSyncResponseDto> Handle(BatchSyncCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        // 1. APPLY INCOMING -----------------------
        var sinceUtc = DateTimeOffset.FromUnixTimeMilliseconds(req.LastSyncVersion).UtcDateTime;

        if (req.ChatMessages?.Count > 0)
        {
            foreach (var dto in req.ChatMessages)
            {
                var existing = await _db.ChatMessages.FirstOrDefaultAsync(c => c.Id == dto.Id, cancellationToken);
                if (existing == null)
                {
                    await _db.ChatMessages.AddAsync(dto.ToEntity(), cancellationToken);
                }
                else if (dto.SentAt.UtcDateTime > existing.CreatedAt)
                {
                    existing.UpdateStatus((Domain.ValueObjects.MessageStatus)dto.Status);
                }
            }
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
            foreach (var dto in req.Restrictions)
            {
                var existing = await _db.Restrictions.FirstOrDefaultAsync(r => r.Id == dto.Id, cancellationToken);
                if (existing == null)
                    await _db.Restrictions.AddAsync(dto.ToEntity(), cancellationToken);
            }
        }

        // Handle incoming PhotoReports (patient -> server)
        if (req.PhotoReports?.Count > 0)
        {
            foreach (var dto in req.PhotoReports)
            {
                var existing = await _db.PhotoReports.FirstOrDefaultAsync(r => r.Id == dto.Id, cancellationToken);
                if (existing == null)
                {
                    var entity = new PhotoReport(
                        dto.Id,
                        dto.PatientId,
                        dto.ImageUrl,
                        dto.ThumbnailUrl,
                        dto.Date,
                        dto.Notes,
                        (HairCarePlus.Server.Domain.ValueObjects.PhotoType)dto.Type);

                    // add nested comments if any
                    if (dto.Comments?.Count > 0)
                    {
                        foreach (var c in dto.Comments)
                        {
                            entity.Comments.Add(new PhotoComment(c.Id!=Guid.Empty?c.Id:Guid.NewGuid(), c.AuthorId, dto.Id, c.Text, c.CreatedAtUtc));
                        }
                    }

                    await _db.PhotoReports.AddAsync(entity, cancellationToken);
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        // 2. COLLECT DELTA -------------------------
        var deltaChat = await _db.ChatMessages
            .Where(c => c.CreatedAt > sinceUtc)
            .Select(c => c.ToDto())
            .ToListAsync(cancellationToken);

        var deltaComments = await _db.PhotoComments
            .Where(c => c.CreatedAtUtc > sinceUtc)
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
        
        var deltaRestrictions = await _db.Restrictions
            .Where(r => r.CreatedAt > sinceUtc)
            .Select(r => r.ToDto())
            .ToListAsync(cancellationToken);

        // ---- PhotoReport differential sync -----------------------------
        List<PhotoReportDto> reportsToSend = new();
        List<Guid> needFromClient = new();

        if (req.PhotoReportHeaders != null)
        {
            var clientMap = req.PhotoReportHeaders.ToDictionary(h => h.Id, h => h.ModifiedAtUtc);

            var serverAll = await _db.PhotoReports.ToListAsync(cancellationToken);
            foreach (var srv in serverAll)
            {
                clientMap.TryGetValue(srv.Id, out var clientMod);
                var serverMod = srv.UpdatedAt ?? srv.CreatedAt;

                // if client has no such id OR server newer – send full dto
                if (!clientMap.ContainsKey(srv.Id) || serverMod > clientMod)
                {
                    reportsToSend.Add(srv.ToDto());
                }
            }

            // Any client headers newer than server?
            foreach (var header in req.PhotoReportHeaders)
            {
                var srv = serverAll.FirstOrDefault(r => r.Id == header.Id);
                if (srv == null || (srv.UpdatedAt ?? srv.CreatedAt) < header.ModifiedAtUtc)
                {
                    needFromClient.Add(header.Id);
                }
            }
        }

        var resp = new BatchSyncResponseDto
        {
            NewSyncVersion = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ChatMessages = deltaChat,
            PhotoComments = deltaComments,
            CalendarTasks = deltaCalendar,
            PhotoReports = reportsToSend,
            NeedPhotoReports = needFromClient,
            ProgressEntries = deltaProgress,
            Restrictions = deltaRestrictions
        };

        return resp;
    }
} 