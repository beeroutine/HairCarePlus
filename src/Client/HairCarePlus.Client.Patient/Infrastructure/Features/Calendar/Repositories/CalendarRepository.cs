using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Domain.Entities;
using HairCarePlus.Client.Patient.Features.Calendar.Domain.Repositories;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Storage;

namespace HairCarePlus.Client.Patient.Infrastructure.Features.Calendar.Repositories;

// Alias enums to avoid ambiguity between domain and storage versions
using DomainEventType = HairCarePlus.Client.Patient.Features.Calendar.Domain.Entities.EventType;
using DomainEventPriority = HairCarePlus.Client.Patient.Features.Calendar.Domain.Entities.EventPriority;
using StorageEventType = HairCarePlus.Client.Patient.Features.Calendar.Models.EventType;
using StorageEventPriority = HairCarePlus.Client.Patient.Features.Calendar.Models.EventPriority;

/// <summary>
/// EF Core backed implementation of <see cref="IHairTransplantEventRepository"/>.
/// Maps between the domain model (<see cref="HairTransplantEvent"/>) and the storage model (<see cref="CalendarEvent"/>).
/// </summary>
public sealed class CalendarRepository : IHairTransplantEventRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private const string LAST_SYNC_PREFS_KEY = "Calendar_LastSyncUtc";

    public CalendarRepository(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
    }

    #region Query helpers

    private static HairTransplantEvent ToDomain(CalendarEvent evt)
    {
        return new HairTransplantEvent
        {
            Id = evt.Id,
            Title = evt.Title,
            Description = evt.Description,
            StartDate = evt.StartDate,
            EndDate = evt.EndDate ?? evt.StartDate,
            CreatedAt = evt.CreatedAt,
            ModifiedAt = evt.ModifiedAt,
            IsCompleted = evt.IsCompleted,
            Notes = evt.Description,
            Type = evt.EventType switch
            {
                StorageEventType.MedicationTreatment => DomainEventType.Medication,
                StorageEventType.MedicalVisit => DomainEventType.MedicalVisit,
                StorageEventType.Photo => DomainEventType.Photo,
                StorageEventType.Video => DomainEventType.Video,
                StorageEventType.CriticalWarning => DomainEventType.Warning,
                _ => DomainEventType.Recommendation
            },
            Priority = evt.Priority switch
            {
                StorageEventPriority.Low => DomainEventPriority.Low,
                StorageEventPriority.Normal => DomainEventPriority.Normal,
                StorageEventPriority.High => DomainEventPriority.High,
                StorageEventPriority.Critical => DomainEventPriority.Critical,
                _ => DomainEventPriority.Normal
            }
        };
    }

    private static CalendarEvent ToStorage(HairTransplantEvent evt)
    {
        return new CalendarEvent
        {
            Id = evt.Id == Guid.Empty ? Guid.NewGuid() : evt.Id,
            Title = evt.Title,
            Description = evt.Description,
            Date = evt.StartDate.Date,
            StartDate = evt.StartDate,
            EndDate = evt.EndDate,
            CreatedAt = evt.CreatedAt == default ? DateTime.UtcNow : evt.CreatedAt,
            ModifiedAt = evt.ModifiedAt == default ? DateTime.UtcNow : evt.ModifiedAt,
            IsCompleted = evt.IsCompleted,
            EventType = evt.Type switch
            {
                DomainEventType.Medication => StorageEventType.MedicationTreatment,
                DomainEventType.MedicalVisit => StorageEventType.MedicalVisit,
                DomainEventType.Photo => StorageEventType.Photo,
                DomainEventType.Video => StorageEventType.Video,
                DomainEventType.Warning => StorageEventType.CriticalWarning,
                _ => StorageEventType.GeneralRecommendation
            },
            Priority = evt.Priority switch
            {
                DomainEventPriority.Low => StorageEventPriority.Low,
                DomainEventPriority.Normal => StorageEventPriority.Normal,
                DomainEventPriority.High => StorageEventPriority.High,
                DomainEventPriority.Critical => StorageEventPriority.Critical,
                _ => StorageEventPriority.Normal
            },
            TimeOfDay = TimeOfDay.Morning,
            ReminderTime = TimeSpan.Zero,
            ExpirationDate = null
        };
    }

    #endregion

    #region IHairTransplantEventRepository implementation

    public async Task<IEnumerable<HairTransplantEvent>> GetEventsForDateAsync(DateTime date)
    {
        var dayStart = date.Date;
        var dayEnd = date.Date.AddDays(1).AddTicks(-1);

        await using var db = _dbFactory.CreateDbContext();
        var events = await db.Events
            .Where(e => e.StartDate <= dayEnd && (e.EndDate ?? e.StartDate) >= dayStart)
            .OrderBy(e => e.StartDate)
            .AsNoTracking()
            .ToListAsync();

        return events.Select(ToDomain);
    }

    public async Task<IEnumerable<HairTransplantEvent>> GetEventsForRangeAsync(DateTime startDate, DateTime endDate)
    {
        var rangeStart = startDate.Date;
        var rangeEnd = endDate.Date.AddDays(1).AddTicks(-1);

        await using var db = _dbFactory.CreateDbContext();
        var events = await db.Events
            .Where(e => e.StartDate <= rangeEnd && (e.EndDate ?? e.StartDate) >= rangeStart)
            .OrderBy(e => e.StartDate)
            .AsNoTracking()
            .ToListAsync();

        return events.Select(ToDomain);
    }

    public async Task<HairTransplantEvent> GetEventByIdAsync(Guid id)
    {
        await using var db = _dbFactory.CreateDbContext();
        var entity = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        return entity == null ? null! : ToDomain(entity);
    }

    public async Task<HairTransplantEvent> AddEventAsync(HairTransplantEvent @event)
    {
        await using var db = _dbFactory.CreateDbContext();
        var storageEntity = ToStorage(@event);
        await db.Events.AddAsync(storageEntity);
        await db.SaveChangesAsync();
        return ToDomain(storageEntity);
    }

    public async Task<HairTransplantEvent> UpdateEventAsync(HairTransplantEvent @event)
    {
        await using var db = _dbFactory.CreateDbContext();
        var existing = await db.Events.FirstOrDefaultAsync(e => e.Id == @event.Id);
        if (existing == null)
            throw new InvalidOperationException($"Event {@event.Id} not found");

        existing.Title = @event.Title;
        existing.Description = @event.Description;
        existing.StartDate = @event.StartDate;
        existing.EndDate = @event.EndDate;
        existing.ModifiedAt = DateTime.UtcNow;
        existing.IsCompleted = @event.IsCompleted;
        existing.EventType = ToStorage(@event).EventType;
        existing.Priority = ToStorage(@event).Priority;

        await db.SaveChangesAsync();
        return ToDomain(existing);
    }

    public async Task<bool> DeleteEventAsync(Guid id)
    {
        await using var db = _dbFactory.CreateDbContext();
        var entity = await db.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (entity == null) return false;

        db.Events.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<HairTransplantEvent>> GetPendingEventsAsync()
    {
        var today = DateTime.UtcNow.Date;
        await using var db = _dbFactory.CreateDbContext();
        var events = await db.Events
            .Where(e => !e.IsCompleted && e.EndDate >= today)
            .OrderBy(e => e.StartDate)
            .AsNoTracking()
            .ToListAsync();

        return events.Select(ToDomain);
    }

    public async Task<bool> MarkEventAsCompletedAsync(Guid id)
    {
        await using var db = _dbFactory.CreateDbContext();
        var entity = await db.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (entity == null) return false;

        entity.IsCompleted = true;
        entity.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public Task<DateTime> GetLastSyncTimeAsync()
    {
        var ticks = Preferences.Get(LAST_SYNC_PREFS_KEY, DateTime.MinValue.Ticks);
        return Task.FromResult(new DateTime(ticks, DateTimeKind.Utc));
    }

    public Task UpdateLastSyncTimeAsync(DateTime syncTime)
    {
        Preferences.Set(LAST_SYNC_PREFS_KEY, syncTime.Ticks);
        return Task.CompletedTask;
    }

    #endregion
} 