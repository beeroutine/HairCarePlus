using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Shared.Communication;
using System.Linq;
using HairCarePlus.Client.Clinic.Infrastructure.Network.Events;
using HairCarePlus.Client.Clinic.Infrastructure.Storage;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using HairCarePlus.Client.Clinic.Features.Sync.Domain.Entities;
using System;
using System.Text.Json;
using System.Threading;
using HairCarePlus.Client.Clinic.Features.Sync.Application;

namespace HairCarePlus.Client.Clinic.Infrastructure.Features.Progress;

    public sealed class PhotoReportService : IPhotoReportService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IMessenger _messenger;
    private readonly IEventsSubscription _events;
        private readonly IOutboxRepository _outbox;
        private readonly ISyncService _syncService;
    private readonly HairCarePlus.Client.Clinic.Infrastructure.FileCache.IFileCacheService _fileCache;
    private bool _handlersAttached = false;
    private string _patientId = string.Empty;

    // simple per-patient cache to avoid redundant DB selects during one ViewModel lifecycle
    private readonly Dictionary<string, IReadOnlyList<PhotoReportDto>> _cache = new();

        public PhotoReportService(IDbContextFactory<AppDbContext> dbFactory,
                                  IMessenger messenger,
                                  IEventsSubscription eventsSub,
                                  IOutboxRepository outbox,
                                  ISyncService syncService,
                                  HairCarePlus.Client.Clinic.Infrastructure.FileCache.IFileCacheService fileCache)
    {
        _dbFactory = dbFactory;
        _messenger = messenger;
        _events = eventsSub;
            _outbox = outbox;
            _syncService = syncService;
        _fileCache = fileCache;
    }

    public async Task<IReadOnlyList<PhotoReportDto>> GetReportsAsync(string patientId)
    {
        if (_cache.TryGetValue(patientId, out var cachedList))
        {
            return cachedList;
        }

        await using var db = _dbFactory.CreateDbContext();
        var entities = await db.PhotoReports
                             .Include(p => p.Comments)
                             .Where(p => p.PatientId == patientId)
                               .AsNoTracking()
                             .ToListAsync();

        var mapped = entities
            .Select(Map)
            .OrderByDescending(r => r.Date)
            .ToList();
        _cache[patientId] = mapped;
        return mapped;
    }

    public async Task<PhotoCommentDto> AddCommentAsync(string patientId, string photoReportId, string authorId, string text)
    {
        Guid.TryParse(photoReportId, out var reportGuid);
        Guid.TryParse(authorId, out var authorGuid);

        var dto = new PhotoCommentDto
        {
            Id = Guid.NewGuid(),
            PhotoReportId = reportGuid == Guid.Empty ? Guid.NewGuid() : reportGuid,
            AuthorId = authorGuid == Guid.Empty ? Guid.NewGuid() : authorGuid,
            Text = text,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        // Persist locally for immediate UI reflection
        await using var db = _dbFactory.CreateDbContext();
        // Enrich DTO with set/type so the patient app can match by SetId when PhotoReportId differs across devices
        var parentReport = await db.PhotoReports.FirstOrDefaultAsync(p => p.Id == photoReportId);
        if (parentReport != null)
        {
            if (!string.IsNullOrWhiteSpace(parentReport.SetId) && Guid.TryParse(parentReport.SetId, out var setGuid))
                dto.SetId = setGuid;
            dto.Type = parentReport.Type;
        }
        // Upsert semantics for doctor's comment per report+author: replace last text
        // SQLite doesn't support ORDER BY DateTimeOffset translation.
        // Fetch and order in-memory to pick the latest author comment for this report.
        var existingList = await db.PhotoComments
            .Where(c => c.PhotoReportId == photoReportId && c.AuthorId == authorId)
            .ToListAsync();
        var existing = existingList
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefault();
        if (existing == null)
        {
            db.PhotoComments.Add(new PhotoCommentEntity
            {
                PhotoReportId = photoReportId,
                AuthorId = authorId,
                Text = text,
                CreatedAtUtc = dto.CreatedAtUtc
            });
        }
        else
        {
            existing.Text = text;
            existing.CreatedAtUtc = dto.CreatedAtUtc;
        }
        // Also store brief summary on the parent report so feed builder can surface it later
        if (parentReport != null)
        {
            parentReport.DoctorComment = text;
        }
        await db.SaveChangesAsync();

        // Invalidate in-memory cache so next GetReportsAsync reflects the new comment
        if (!string.IsNullOrEmpty(patientId))
        {
            _cache.Remove(patientId);
        }

        // Enqueue to Outbox for reliable sync to server → patient
        await _outbox.AddAsync(new OutboxItemDto
        {
            EntityType = "PhotoComment",
            Payload = JsonSerializer.Serialize(dto),
            LocalEntityId = dto.PhotoReportId.ToString(),
            ModifiedAtUtc = DateTime.UtcNow
        });

        // Trigger sync in background (do not await to avoid blocking UI editor)
        try { _ = _syncService.SynchronizeAsync(CancellationToken.None); } catch { }

        return dto;
    }

    private static PhotoReportDto Map(PhotoReportEntity e)
    {
        Guid.TryParse(e.Id, out var repId);
        Guid.TryParse(e.PatientId, out var pid);
        Guid? setId = null;
        if (!string.IsNullOrWhiteSpace(e.SetId) && Guid.TryParse(e.SetId, out var setGuid))
            setId = setGuid;

        // resolve LocalPath (may be relative) and decide which path to expose to UI
        string? resolvedLocal = null;
        if (!string.IsNullOrWhiteSpace(e.LocalPath))
        {
            resolvedLocal = e.LocalPath;
            if (!Path.IsPathRooted(resolvedLocal))
            {
                try
                {
                    var baseDir = Microsoft.Maui.Storage.FileSystem.AppDataDirectory;
                    resolvedLocal = Path.Combine(baseDir, resolvedLocal);
                }
                catch { }
            }
        }

        // prefer existing local file; otherwise UI can fall back to ImageUrl (HTTP) via converter
        var localForUi = !string.IsNullOrWhiteSpace(resolvedLocal) && System.IO.File.Exists(resolvedLocal)
            ? resolvedLocal
            : null;

        return new PhotoReportDto
        {
            Id = repId == Guid.Empty ? Guid.NewGuid() : repId,
            PatientId = pid == Guid.Empty ? Guid.NewGuid() : pid,
            SetId = setId,
            ImageUrl = e.ImageUrl,
            LocalPath = localForUi ?? (
                !string.IsNullOrWhiteSpace(e.ImageUrl) &&
                (e.ImageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || e.ImageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    ? e.ImageUrl
                    : e.LocalPath
            ),
            Date = e.Date,
            Notes = e.DoctorComment ?? string.Empty,
            Type = e.Type,
            Comments = e.Comments.Select(c =>
            {
                Guid.TryParse(c.PhotoReportId, out var prId);
                Guid.TryParse(c.AuthorId, out var authId);
                return new PhotoCommentDto
                {
                    PhotoReportId = prId == Guid.Empty ? Guid.NewGuid() : prId,
                    AuthorId = authId == Guid.Empty ? Guid.NewGuid() : authId,
                    Text = c.Text,
                    CreatedAtUtc = c.CreatedAtUtc
                };
            }).ToList()
        };
    }

    // ensure SignalR subscribed
    public async Task ConnectAsync(string patientId)
    {
        _patientId = patientId;
        // Proactively drop stale cache so the first UI load after navigation always queries DB
        if (!string.IsNullOrEmpty(patientId))
        {
            _cache.Remove(patientId);
        }
        await _events.ConnectAsync(patientId);

        if(!_handlersAttached)
        {
            _events.PhotoReportAdded += OnPhotoReportAdded;
            _events.PhotoCommentAdded += OnPhotoCommentAdded;
            // Важно: сервер пушит PhotoReportSetAdded, а не три отдельных события
            _events.PhotoReportSetAdded += OnPhotoReportSetAdded;
            _handlersAttached=true;
        }
    }

    private async void OnPhotoReportSetAdded(object? sender, PhotoReportSetDto set)
    {
        try
        {
            // Преобразуем набор в три photo reports в локальную БД и инвалидируем кэш, чтобы UI перерисовался
            if (set == null || set.Items == null || set.Items.Count == 0) return;

            await using var db = _dbFactory.CreateDbContext();
            foreach (var it in set.Items)
            {
                var id = it.Id?.ToString() ?? Guid.NewGuid().ToString();
                var exists = await db.PhotoReports.AnyAsync(p => p.Id == id);
                if (exists) continue;
                var entity = new PhotoReportEntity
                {
                    Id = id,
                    PatientId = set.PatientId.ToString(),
                    SetId = set.Id.ToString(),
                    ImageUrl = it.ImageUrl,
                    LocalPath = null,
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
                        var path = await _fileCache.GetLocalPathAsync(it.ImageUrl, CancellationToken.None);
                        if (path != null)
                            entity.LocalPath = path;
                    }
                }
                catch { }
                await db.PhotoReports.AddAsync(entity);
            }
            await db.SaveChangesAsync();

            if(!string.IsNullOrEmpty(_patientId))
                _cache.Remove(_patientId);
        }
        catch { }
    }

    private async void OnPhotoReportAdded(object? sender, PhotoReportDto dto)
    {
        await using var ctx = _dbFactory.CreateDbContext();
        var entity = await ctx.PhotoReports.Include(p=>p.Comments).FirstOrDefaultAsync(p=>p.Id==dto.Id.ToString());
        if(entity==null)
        {
            entity = new PhotoReportEntity
            {
                Id = dto.Id.ToString(),
                PatientId = _patientId,
                ImageUrl = dto.ImageUrl,
                Date = dto.Date,
                DoctorComment = dto.Notes
            };
            ctx.PhotoReports.Add(entity);
        }
        ctx.SaveChanges();

        // invalidate cache for patient so subsequent GetReportsAsync returns fresh data
        if(!string.IsNullOrEmpty(_patientId))
            _cache.Remove(_patientId);
    }

    private async void OnPhotoCommentAdded(object? sender, PhotoCommentDto dto)
    {
        await using var ctx = _dbFactory.CreateDbContext();
        var report = await ctx.PhotoReports.FirstOrDefaultAsync(r=>r.Id==dto.PhotoReportId.ToString());
        if(report==null) return;
        ctx.PhotoComments.Add(new PhotoCommentEntity
        {
            PhotoReportId = dto.PhotoReportId.ToString(),
            AuthorId = dto.AuthorId.ToString(),
            Text = dto.Text,
            CreatedAtUtc = dto.CreatedAtUtc
        });
        ctx.SaveChanges();
    }
} 