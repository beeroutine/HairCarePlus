using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Infrastructure.Storage;
using HairCarePlus.Shared.Communication;
using HairCarePlus.Shared.Communication.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Messaging;

namespace HairCarePlus.Client.Patient.Features.Sync.Infrastructure;

public interface ISyncChangeApplier
{
    Task ApplyAsync(BatchSyncResponseDto response);
}

public sealed class SyncChangeApplier : ISyncChangeApplier
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IMessenger _messenger;
    private readonly ILogger<SyncChangeApplier> _logger;

    public SyncChangeApplier(IDbContextFactory<AppDbContext> dbFactory, IMessenger messenger, ILogger<SyncChangeApplier> logger)
    {
        _dbFactory = dbFactory;
        _messenger = messenger;
        _logger = logger;
    }

    public async Task ApplyAsync(BatchSyncResponseDto response)
    {
        await using var db = _dbFactory.CreateDbContext();
        var changesApplied = 0;

        // Patient ignores incoming PhotoReports – only Clinic needs them
        /* intentionally skipped */

        if (response.Changes.TryGetValue("PhotoComment", out var comments))
        {
            foreach (var obj in comments)
            {
                var elem = (JsonElement)obj;
                var dto = elem.Deserialize<PhotoCommentDto>();
                if (dto == null) continue;

                // ensure parent report exists
                var parentId = dto.PhotoReportId.ToString();
                var exists = await db.PhotoReports.AnyAsync(p => p.Id == parentId);
                if (!exists) continue; // or create stub

                var duplicate = await db.PhotoComments.AnyAsync(c => c.PhotoReportId == parentId && c.AuthorId == dto.AuthorId.ToString() && c.Text == dto.Text && c.CreatedAtUtc == dto.CreatedAtUtc);
                if (duplicate) continue;

                var newEntity = new Domain.Entities.PhotoCommentEntity
                {
                    PhotoReportId = parentId,
                    AuthorId = dto.AuthorId.ToString(),
                    Text = dto.Text,
                    CreatedAtUtc = dto.CreatedAtUtc
                };
                db.PhotoComments.Add(newEntity);
                changesApplied++;
                _messenger.Send(new Messages.PhotoCommentSyncedMessage(newEntity));
            }
        }

        // --- New typed list handling (server may populate typed properties instead of dictionary) ---
        if ((response.PhotoReports?.Count ?? 0) > 0)
        {
            foreach (var dto in response.PhotoReports!)
            {
                var reportId = dto.Id.ToString();
                var entity = await db.PhotoReports.Include(p => p.Comments)
                                                  .FirstOrDefaultAsync(p => p.Id == reportId);
                if (entity == null)
                {
                    entity = new Domain.Entities.PhotoReportEntity
                    {
                        Id = reportId,
                        ImageUrl = dto.ImageUrl,
                        CaptureDate = dto.Date,
                        DoctorComment = dto.Notes,
                        PatientId = dto.PatientId.ToString()
                    };
                    db.PhotoReports.Add(entity);
                }
                else
                {
                    entity.ImageUrl = dto.ImageUrl;
                    entity.CaptureDate = dto.Date;
                    entity.DoctorComment = dto.Notes;
                    entity.PatientId = dto.PatientId.ToString();
                }
                changesApplied++;
                _messenger.Send(new Messages.PhotoReportSyncedMessage(entity));
            }
        }

        if ((response.PhotoComments?.Count ?? 0) > 0)
        {
            foreach (var dto in response.PhotoComments!)
            {
                var parentId = dto.PhotoReportId.ToString();
                var exists = await db.PhotoReports.AnyAsync(p => p.Id == parentId);
                if (!exists) continue;

                var duplicate = await db.PhotoComments.AnyAsync(c => c.PhotoReportId == parentId && c.AuthorId == dto.AuthorId.ToString() && c.Text == dto.Text && c.CreatedAtUtc == dto.CreatedAtUtc);
                if (duplicate) continue;

                var newEntity = new Domain.Entities.PhotoCommentEntity
                {
                    PhotoReportId = parentId,
                    AuthorId = dto.AuthorId.ToString(),
                    Text = dto.Text,
                    CreatedAtUtc = dto.CreatedAtUtc
                };
                db.PhotoComments.Add(newEntity);
                changesApplied++;
                _messenger.Send(new Messages.PhotoCommentSyncedMessage(newEntity));
            }
        }

        if (changesApplied > 0)
        {
            await db.SaveChangesAsync();
            _logger.LogInformation("Applied {Count} server changes", changesApplied);
        }
    }
} 