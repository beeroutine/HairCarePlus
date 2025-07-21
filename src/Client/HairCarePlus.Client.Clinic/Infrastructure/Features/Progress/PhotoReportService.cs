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

namespace HairCarePlus.Client.Clinic.Infrastructure.Features.Progress;

public sealed class PhotoReportService : IPhotoReportService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IMessenger _messenger;
    private readonly IEventsSubscription _events;
    private bool _handlersAttached = false;
    private string _patientId = string.Empty;

    // simple per-patient cache to avoid redundant DB selects during one ViewModel lifecycle
    private readonly Dictionary<string, IReadOnlyList<PhotoReportDto>> _cache = new();

    public PhotoReportService(IDbContextFactory<AppDbContext> dbFactory, IMessenger messenger, IEventsSubscription eventsSub)
    {
        _dbFactory = dbFactory;
        _messenger = messenger;
        _events = eventsSub;
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

        var mapped = entities.Select(Map).ToList();
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

        // TODO: вместо прямого POST создаём OutboxItem; пока просто сохраняем локально
        await using var db = _dbFactory.CreateDbContext();
        db.PhotoComments.Add(new PhotoCommentEntity
        {
            PhotoReportId = photoReportId,
            AuthorId = authorId,
            Text = text,
            CreatedAtUtc = dto.CreatedAtUtc
        });
        await db.SaveChangesAsync();
        return dto;
    }

    private static PhotoReportDto Map(PhotoReportEntity e)
    {
        Guid.TryParse(e.Id, out var repId);
        Guid.TryParse(e.PatientId, out var pid);

        // Fallback: если remote ImageUrl приведёт к 404, отдаём локальный путь (если файл существует)
        var preferredUrl = e.ImageUrl;
        if (!string.IsNullOrWhiteSpace(e.LocalPath) && System.IO.File.Exists(e.LocalPath))
        {
            // if remote is empty или явно localhost адрес (предположительно может отсутствовать), используем локальный файл
            if (string.IsNullOrWhiteSpace(preferredUrl))
            {
                preferredUrl = e.LocalPath;
            }
        }

        return new PhotoReportDto
        {
            Id = repId == Guid.Empty ? Guid.NewGuid() : repId,
            PatientId = pid == Guid.Empty ? Guid.NewGuid() : pid,
            ImageUrl = preferredUrl,
            LocalPath = e.LocalPath,
            Date = e.Date,
            Notes = e.DoctorComment ?? string.Empty,
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
        await _events.ConnectAsync(patientId);

        if(!_handlersAttached)
        {
            _events.PhotoReportAdded += OnPhotoReportAdded;
            _events.PhotoCommentAdded += OnPhotoCommentAdded;
            _handlersAttached=true;
        }
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