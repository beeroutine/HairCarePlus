using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using HairCarePlus.Client.Clinic.Infrastructure.Storage;
using HairCarePlus.Shared.Communication;
using HairCarePlus.Shared.Communication.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Messaging;
using System.Linq;

namespace HairCarePlus.Client.Clinic.Features.Sync.Infrastructure;

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
        var applied = 0;

        // Handle typed lists first (preferred)
        if ((response.PhotoReports?.Count ?? 0) > 0)
        {
            foreach (var dto in response.PhotoReports!)
            {
                var id = dto.Id.ToString();
                var entity = await db.PhotoReports.Include(p => p.Comments).FirstOrDefaultAsync(p => p.Id == id);
                if (entity == null)
                {
                    entity = new Domain.Entities.PhotoReportEntity
                    {
                        Id = id,
                        PatientId = dto.PatientId.ToString(),
                        ImageUrl = dto.ImageUrl,
                        Date = dto.Date,
                        DoctorComment = dto.Notes
                    };
                    db.PhotoReports.Add(entity);
                }
                else
                {
                    entity.ImageUrl = dto.ImageUrl;
                    entity.Date = dto.Date;
                    entity.DoctorComment = dto.Notes;
                }
                applied++;
                _messenger.Send(dto); // broadcast as needed (placeholder)
            }
        }

        if ((response.Restrictions?.Count ?? 0) > 0)
        {
            _logger.LogInformation("Clinic SyncApplier: applying {Count} restrictions", response.Restrictions!.Count);

            // Track ids that were already handled during this ApplyAsync run to avoid duplicates coming
            // in a single response payload (server bug). Otherwise we may attempt to insert the same key
            // multiple times before SaveChanges and hit UNIQUE constraint violations.
            var handledRestrictionIds = new HashSet<string>();

            foreach (var dto in response.Restrictions!)
            {
                var id = dto.Id.ToString();

                // Skip duplicates contained in the same response
                if (!handledRestrictionIds.Add(id))
                    continue;

                // First look inside the current DbContext change tracker – the entity may have been
                // added earlier in this loop but not yet persisted.
                var tracked = db.ChangeTracker.Entries<Domain.Entities.RestrictionEntity>()
                                    .FirstOrDefault(e => e.Entity.Id == id)?.Entity;

                var entity = tracked ?? await db.Restrictions.FirstOrDefaultAsync(r => r.Id == id);

                // Fallback: try to find by patient/type/dates (handles client-generated duplicate IDs)
                if (entity == null)
                {
                    entity = await db.Restrictions.FirstOrDefaultAsync(r =>
                        r.PatientId == dto.PatientId.ToString() &&
                        r.Type == (int)dto.IconType &&
                        r.StartUtc == dto.StartUtc &&
                        r.EndUtc == dto.EndUtc);
                }

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
                    db.Restrictions.Add(entity);
                }
                else
                {
                    // Update existing record in case any fields changed
                    entity.Type = (int)dto.IconType;
                    entity.StartUtc = dto.StartUtc;
                    entity.EndUtc = dto.EndUtc;
                    entity.IsActive = dto.IsActive;
                }
                applied++;
                _messenger.Send(dto); // broadcast for UI update
            }
        }

        if ((response.PhotoComments?.Count ?? 0) > 0)
        {
            foreach (var dto in response.PhotoComments!)
            {
                var prId = dto.PhotoReportId.ToString();
                if (!await db.PhotoReports.AnyAsync(p => p.Id == prId)) continue;

                var exists = await db.PhotoComments.AnyAsync(c => c.PhotoReportId == prId && c.AuthorId == dto.AuthorId.ToString() && c.Text == dto.Text && c.CreatedAtUtc == dto.CreatedAtUtc);
                if (exists) continue;

                var comment = new Domain.Entities.PhotoCommentEntity
                {
                    PhotoReportId = prId,
                    AuthorId = dto.AuthorId.ToString(),
                    Text = dto.Text,
                    CreatedAtUtc = dto.CreatedAtUtc
                };
                db.PhotoComments.Add(comment);
                applied++;
                _messenger.Send(dto);
            }
        }

        if (applied > 0)
        {
            await db.SaveChangesAsync();
            _logger.LogInformation("Clinic SyncApplier applied {Count} changes", applied);
        }
    }
} 