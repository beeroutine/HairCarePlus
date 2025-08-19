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

                // Upsert comment from the same author for the same report (edit)
                var existing = await db.PhotoComments
                    .Where(c => c.PhotoReportId == parentId && c.AuthorId == dto.AuthorId.ToString())
                    .OrderByDescending(c => c.CreatedAtUtc)
                    .FirstOrDefaultAsync();
                Domain.Entities.PhotoCommentEntity newEntity;
                if (existing == null)
                {
                    newEntity = new Domain.Entities.PhotoCommentEntity
                    {
                        PhotoReportId = parentId,
                        AuthorId = dto.AuthorId.ToString(),
                        Text = dto.Text,
                        CreatedAtUtc = dto.CreatedAtUtc
                    };
                    db.PhotoComments.Add(newEntity);
                }
                else
                {
                    existing.Text = dto.Text;
                    existing.CreatedAtUtc = dto.CreatedAtUtc;
                    newEntity = existing;
                }
                changesApplied++;
                _messenger.Send(new Messages.PhotoCommentSyncedMessage(newEntity));
                // Also mirror summary on the report so feed queries show doctor comment immediately
                var parentReport = await db.PhotoReports.FirstOrDefaultAsync(p => p.Id == parentId);
                if (parentReport != null)
                {
                    parentReport.DoctorComment = dto.Text;
                }
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

                var existing2 = await db.PhotoComments
                    .Where(c => c.PhotoReportId == parentId && c.AuthorId == dto.AuthorId.ToString())
                    .OrderByDescending(c => c.CreatedAtUtc)
                    .FirstOrDefaultAsync();
                Domain.Entities.PhotoCommentEntity newEntity;
                if (existing2 == null)
                {
                    newEntity = new Domain.Entities.PhotoCommentEntity
                    {
                        PhotoReportId = parentId,
                        AuthorId = dto.AuthorId.ToString(),
                        Text = dto.Text,
                        CreatedAtUtc = dto.CreatedAtUtc
                    };
                    db.PhotoComments.Add(newEntity);
                }
                else
                {
                    existing2.Text = dto.Text;
                    existing2.CreatedAtUtc = dto.CreatedAtUtc;
                    newEntity = existing2;
                }
                changesApplied++;
                _messenger.Send(new Messages.PhotoCommentSyncedMessage(newEntity));
                var parentReport2 = await db.PhotoReports.FirstOrDefaultAsync(p => p.Id == parentId);
                if (parentReport2 != null)
                {
                    parentReport2.DoctorComment = dto.Text;
                }
            }
        }

        if (changesApplied > 0)
        {
            await db.SaveChangesAsync();
            _logger.LogInformation("Applied {Count} server changes", changesApplied);
        }
    }
} 