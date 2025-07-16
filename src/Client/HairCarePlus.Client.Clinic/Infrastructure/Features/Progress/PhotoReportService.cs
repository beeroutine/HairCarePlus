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

    public PhotoReportService(IDbContextFactory<AppDbContext> dbFactory, IMessenger messenger, IEventsSubscription eventsSub)
    {
        _dbFactory = dbFactory;
        _messenger = messenger;
        _events = eventsSub;
    }

    public async Task<IReadOnlyList<PhotoReportDto>> GetReportsAsync(string patientId)
    {
        await using var db = _dbFactory.CreateDbContext();
        var cached = await db.PhotoReports
                             .Include(p => p.Comments)
                             .Where(p => p.PatientId == patientId)
                             .ToListAsync();

        // возвращаем кэшированные данные; они будут обновляться через BatchSync/SignalR
        return cached.Select(Map).ToList();
    }

    public async Task<PhotoCommentDto> AddCommentAsync(string patientId, string photoReportId, string authorId, string text)
    {
        var dto = new PhotoCommentDto
        {
            Id = Guid.NewGuid(),
            PhotoReportId = Guid.Parse(photoReportId),
            AuthorId = Guid.Parse(authorId),
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
        return new PhotoReportDto
        {
            Id = Guid.Parse(e.Id),
            PatientId = Guid.Parse(e.PatientId),
            ImageUrl = e.ImageUrl,
            LocalPath = e.LocalPath,
            Date = e.Date,
            Notes = e.DoctorComment ?? string.Empty,
            Comments = e.Comments.Select(c => new PhotoCommentDto
            {
                PhotoReportId = Guid.Parse(c.PhotoReportId),
                AuthorId = Guid.Parse(c.AuthorId),
                Text = c.Text,
                CreatedAtUtc = c.CreatedAtUtc
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