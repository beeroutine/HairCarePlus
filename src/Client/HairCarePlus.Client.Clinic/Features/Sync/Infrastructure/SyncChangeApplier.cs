using CommunityToolkit.Mvvm.Messaging;
using HairCarePlus.Client.Clinic.Infrastructure.Storage;
using HairCarePlus.Shared.Communication;
using HairCarePlus.Shared.Communication.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HairCarePlus.Client.Clinic.Features.Sync.Infrastructure
{
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
            await using var db = await _dbFactory.CreateDbContextAsync();
        var applied = 0;

            // Step 1: Collect all IDs from both DTO lists and packets
            var reportIds = response.PhotoReports?.Select(r => r.Id.ToString()).ToList() ?? new List<string>();
            var restrictionIds = response.Restrictions?.Select(r => r.Id.ToString()).ToList() ?? new List<string>();
            
            if (response.Packets != null)
            {
                foreach (var packet in response.Packets)
                {
                    if (packet.EntityType == nameof(PhotoReportDto))
                    {
                        var dto = JsonSerializer.Deserialize<PhotoReportDto>(packet.PayloadJson);
                        if (dto != null) reportIds.Add(dto.Id.ToString());
                    }
                    else if (packet.EntityType == nameof(RestrictionDto))
                    {
                        var dto = JsonSerializer.Deserialize<RestrictionDto>(packet.PayloadJson);
                        if (dto != null) restrictionIds.Add(dto.Id.ToString());
                    }
                }
            }

            // Step 2: Batch fetch all existing entities from the database
            var existingReports = await db.PhotoReports
                .Include(p => p.Comments)
                .Where(p => reportIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            var existingRestrictions = await db.Restrictions
                .Where(r => restrictionIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id);

            // Step 3: Process DTOs from typed lists
            if (response.PhotoReports != null)
            {
                foreach (var dto in response.PhotoReports)
                {
                    applied += ApplyPhotoReport(db, dto, existingReports);
                }
            }
            if (response.Restrictions != null)
            {
                 _logger.LogInformation("Clinic SyncApplier: applying {Count} restrictions from typed list", response.Restrictions.Count);
                foreach (var dto in response.Restrictions)
                {
                    applied += ApplyRestriction(db, dto, existingRestrictions);
                }
            }

            // Step 4: Process DTOs from packets
            if (response.Packets != null)
            {
                foreach (var packet in response.Packets)
                {
                    if (packet.EntityType == nameof(PhotoReportDto))
                    {
                        var dto = JsonSerializer.Deserialize<PhotoReportDto>(packet.PayloadJson);
                        if(dto != null) applied += ApplyPhotoReport(db, dto, existingReports);
                    }
                    else if (packet.EntityType == nameof(RestrictionDto))
                    {
                        var dto = JsonSerializer.Deserialize<RestrictionDto>(packet.PayloadJson);
                        if(dto != null) applied += ApplyRestriction(db, dto, existingRestrictions);
                    }
                }
            }
            
            if (applied > 0)
            {
                await db.SaveChangesAsync();
                _logger.LogInformation("Clinic SyncApplier applied {Count} changes", applied);
            }
        }

        private int ApplyPhotoReport(AppDbContext db, PhotoReportDto dto, Dictionary<string, Domain.Entities.PhotoReportEntity> existingReports)
        {
            var id = dto.Id.ToString();
            if (existingReports.TryGetValue(id, out var entity))
            {
                entity.ImageUrl = dto.ImageUrl;
                entity.Date = dto.Date;
                entity.DoctorComment = dto.Notes;
            }
            else
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
                existingReports.Add(id, entity); // Add to dictionary to avoid re-adding
            }
            _messenger.Send(dto);
            return 1;
        }

        private int ApplyRestriction(AppDbContext db, RestrictionDto dto, Dictionary<string, Domain.Entities.RestrictionEntity> existingRestrictions)
        {
            var id = dto.Id.ToString();
            if (existingRestrictions.TryGetValue(id, out var entity))
            {
                entity.Type = (int)dto.IconType;
                entity.StartUtc = dto.StartUtc;
                entity.EndUtc = dto.EndUtc;
                entity.IsActive = dto.IsActive;
            }
            else
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
                existingRestrictions.Add(id, entity); // Add to dictionary to avoid re-adding
            }
                _messenger.Send(dto);
            return 1;
        }
    }
} 