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
            _logger.LogInformation("[CLINIC-SYNC-APPLIER] Starting ApplyAsync - Restrictions: {RestrictionsCount}, PhotoReports: {ReportsCount}, Packets: {PacketsCount}",
                response.Restrictions?.Count ?? 0,
                response.PhotoReports?.Count ?? 0,
                response.Packets?.Count ?? 0);
            
            await using var db = await _dbFactory.CreateDbContextAsync();
        var applied = 0;

            // Step 1: Collect all IDs from both DTO lists and packets
            var reportIds = response.PhotoReports?.Select(r => r.Id.ToString()).ToList() ?? new List<string>();
            var restrictionIds = response.Restrictions?.Select(r => r.Id.ToString()).ToList() ?? new List<string>();
            
            if (response.Packets != null)
            {
                _logger.LogInformation("[CLINIC-SYNC-APPLIER] Processing {Count} packets", response.Packets.Count);
                foreach (var packet in response.Packets)
                {
                    _logger.LogDebug("[CLINIC-SYNC-APPLIER] Packet: Type={Type}, Id={Id}", packet.EntityType, packet.Id);
                    if (packet.EntityType == nameof(PhotoReportDto))
                    {
                        var dto = JsonSerializer.Deserialize<PhotoReportDto>(packet.PayloadJson);
                        if (dto != null) 
                        {
                            reportIds.Add(dto.Id.ToString());
                            _logger.LogDebug("[CLINIC-SYNC-APPLIER] Added PhotoReport ID {Id} to processing list", dto.Id);
                        }
                    }
                    else if (packet.EntityType == nameof(RestrictionDto))
                    {
                        var dto = JsonSerializer.Deserialize<RestrictionDto>(packet.PayloadJson);
                        if (dto != null) 
                        {
                            restrictionIds.Add(dto.Id.ToString());
                            _logger.LogInformation("[CLINIC-SYNC-APPLIER] Found Restriction packet: Id={Id}, PatientId={PatientId}, Type={Type}, Active={Active}",
                                dto.Id, dto.PatientId, dto.IconType, dto.IsActive);
                        }
                    }
                    else if (packet.EntityType == "Restriction")
                    {
                        // Handle packets with EntityType="Restriction" (not "RestrictionDto")
                        var dto = JsonSerializer.Deserialize<RestrictionDto>(packet.PayloadJson);
                        if (dto != null) 
                        {
                            restrictionIds.Add(dto.Id.ToString());
                            _logger.LogInformation("[CLINIC-SYNC-APPLIER] Found Restriction packet (non-Dto type): Id={Id}, PatientId={PatientId}, Type={Type}, Active={Active}",
                                dto.Id, dto.PatientId, dto.IconType, dto.IsActive);
                        }
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
            // Ephemeral policy: ignore typed PhotoReports list to prevent history reconstruction
            // if (response.PhotoReports != null) { ... }
            if (response.Restrictions != null)
            {
                 _logger.LogInformation("[CLINIC-SYNC-APPLIER] Processing {Count} restrictions from typed list", response.Restrictions.Count);
                foreach (var dto in response.Restrictions)
                {
                    _logger.LogDebug("[CLINIC-SYNC-APPLIER] Applying restriction from list: Id={Id}, PatientId={PatientId}, Type={Type}",
                        dto.Id, dto.PatientId, dto.IconType);
                    applied += ApplyRestriction(db, dto, existingRestrictions);
                }
            }

            // Step 4: Process DTOs from packets
            if (response.Packets != null)
            {
                _logger.LogInformation("[CLINIC-SYNC-APPLIER] Processing packets for persistence");
                foreach (var packet in response.Packets)
                {
                    if (packet.EntityType == nameof(PhotoReportDto))
                    {
                        var dto = JsonSerializer.Deserialize<PhotoReportDto>(packet.PayloadJson);
                        if(dto != null) 
                        {
                            _logger.LogDebug("[CLINIC-SYNC-APPLIER] Applying PhotoReport from packet: Id={Id}", dto.Id);
                            applied += ApplyPhotoReport(db, dto, existingReports);
                        }
                    }
                    else if (packet.EntityType == nameof(RestrictionDto) || packet.EntityType == "Restriction")
                    {
                        var dto = JsonSerializer.Deserialize<RestrictionDto>(packet.PayloadJson);
                        if(dto != null) 
                        {
                            _logger.LogInformation("[CLINIC-SYNC-APPLIER] Applying Restriction from packet: Id={Id}, PatientId={PatientId}, Type={Type}, Active={Active}",
                                dto.Id, dto.PatientId, dto.IconType, dto.IsActive);
                            applied += ApplyRestriction(db, dto, existingRestrictions);
                        }
                    }
                }
            }
            
            if (applied > 0)
            {
                await db.SaveChangesAsync();
                _logger.LogInformation("[CLINIC-SYNC-APPLIER] Successfully saved {Count} changes to database", applied);
            }
            else
            {
                _logger.LogInformation("[CLINIC-SYNC-APPLIER] No changes to apply");
            }
        }

        private int ApplyPhotoReport(AppDbContext db, PhotoReportDto dto, Dictionary<string, Domain.Entities.PhotoReportEntity> existingReports)
        {
            var id = dto.Id.ToString();
            if (existingReports.TryGetValue(id, out var entity))
            {
                entity.ImageUrl = dto.ImageUrl;
                entity.Date = dto.Date;
                // Важно: не перезаписывать локально отредактированный комментарий врача
                // входящими Notes из пациента. Обновляем сводку только если она пустая.
                if (string.IsNullOrWhiteSpace(entity.DoctorComment) && !string.IsNullOrWhiteSpace(dto.Notes))
                {
                    entity.DoctorComment = dto.Notes;
                }
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
            // Use stable, deterministic key per patient + type so repeated packets update the same row
            var id = dto.Id != System.Guid.Empty
                ? dto.Id.ToString()
                : $"{dto.PatientId:N}-{(int)dto.IconType}";
            
            _logger.LogDebug("[CLINIC-SYNC-APPLIER] ApplyRestriction: Processing Id={Id}, PatientId={PatientId}, Type={Type}, Active={Active}",
                id, dto.PatientId, dto.IconType, dto.IsActive);
            if (existingRestrictions.TryGetValue(id, out var entity))
            {
                _logger.LogDebug("[CLINIC-SYNC-APPLIER] Updating existing restriction {Id}", id);
                entity.Type = (int)dto.IconType;
                entity.StartUtc = dto.StartUtc;
                entity.EndUtc = dto.EndUtc;
                entity.IsActive = dto.IsActive;
            }
            else
                {
                    _logger.LogInformation("[CLINIC-SYNC-APPLIER] Creating new restriction: Id={Id}, PatientId={PatientId}, Type={Type}, Active={Active}",
                        id, dto.PatientId, dto.IconType, dto.IsActive);
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