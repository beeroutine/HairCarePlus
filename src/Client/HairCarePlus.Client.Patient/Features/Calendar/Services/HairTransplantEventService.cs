using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Domain.Entities;
using HairCarePlus.Client.Patient.Features.Calendar.Domain.Repositories;
using HairCarePlus.Client.Patient.Infrastructure.Storage;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using System.Linq;
// alias enums to avoid ambiguity between domain and storage
using DomainEventType = HairCarePlus.Client.Patient.Features.Calendar.Domain.Entities.EventType;
using DomainEventPriority = HairCarePlus.Client.Patient.Features.Calendar.Domain.Entities.EventPriority;
using StorageEventType = HairCarePlus.Client.Patient.Features.Calendar.Models.EventType;
using StorageEventPriority = HairCarePlus.Client.Patient.Features.Calendar.Models.EventPriority;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services;

public class HairTransplantEventService : IHairTransplantEventService, ICalendarService
{
    private readonly ICalendarRepository _repository;
    private readonly ILocalStorageService _localStorage;
    private const string SYNC_TIME_KEY = "calendar_last_sync";

    public HairTransplantEventService(
        ICalendarRepository repository,
        ILocalStorageService localStorage)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
    }

    public async Task<IEnumerable<HairTransplantEvent>> GetEventsForDateAsync(DateTime date)
    {
        return await _repository.GetEventsForDateAsync(date);
    }

    public async Task<IEnumerable<HairTransplantEvent>> GetEventsForRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _repository.GetEventsForRangeAsync(startDate, endDate);
    }

    public async Task<HairTransplantEvent> GetEventByIdAsync(Guid id)
    {
        return await _repository.GetEventByIdAsync(id);
    }

    public async Task<HairTransplantEvent> CreateEventAsync(HairTransplantEvent @event)
    {
        @event.CreatedAt = DateTime.UtcNow;
        return await _repository.AddEventAsync(@event);
    }

    public async Task<HairTransplantEvent> UpdateEventAsync(HairTransplantEvent @event)
    {
        @event.ModifiedAt = DateTime.UtcNow;
        return await _repository.UpdateEventAsync(@event);
    }

    public async Task<bool> DeleteEventAsync(Guid id)
    {
        return await _repository.DeleteEventAsync(id);
    }

    public async Task<IEnumerable<HairTransplantEvent>> GetPendingEventsAsync()
    {
        return await _repository.GetPendingEventsAsync();
    }

    public async Task<bool> MarkEventAsCompletedAsync(Guid id)
    {
        return await _repository.MarkEventAsCompletedAsync(id);
    }

    public async Task<DateTime> GetLastSyncTimeAsync()
    {
        return await _repository.GetLastSyncTimeAsync();
    }

    public async Task UpdateLastSyncTimeAsync(DateTime syncTime)
    {
        await _repository.UpdateLastSyncTimeAsync(syncTime);
        await _localStorage.SetLastSyncTimeAsync(SYNC_TIME_KEY, syncTime);
    }

    #region Mapping helpers
    private static CalendarEvent MapToCalendarEvent(HairTransplantEvent e)
    {
        return new CalendarEvent
        {
            Id = e.Id,
            Title = e.Title,
            Description = e.Notes ?? string.Empty,
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            Date = e.StartDate.Date,
            CreatedAt = e.CreatedAt,
            ModifiedAt = e.ModifiedAt,
            IsCompleted = e.IsCompleted,
            EventType = e.Type switch
            {
                DomainEventType.Medication => StorageEventType.MedicationTreatment,
                DomainEventType.MedicalVisit => StorageEventType.MedicalVisit,
                DomainEventType.Photo => StorageEventType.Photo,
                DomainEventType.Video => StorageEventType.Video,
                DomainEventType.Warning => StorageEventType.CriticalWarning,
                _ => StorageEventType.GeneralRecommendation
            },
            Priority = e.Type switch
            {
                DomainEventType.Warning => StorageEventPriority.Critical,
                DomainEventType.Medication => StorageEventPriority.High,
                DomainEventType.MedicalVisit => StorageEventPriority.High,
                DomainEventType.Photo => StorageEventPriority.Normal,
                DomainEventType.Video => StorageEventPriority.Normal,
                _ => StorageEventPriority.Normal
            },
            TimeOfDay = TimeOfDay.Morning,
            ReminderTime = TimeSpan.Zero
        };
    }

    private static HairTransplantEvent MapToDomain(CalendarEvent evt)
    {
        return new HairTransplantEvent
        {
            Id = evt.Id,
            Title = evt.Title,
            Notes = evt.Description,
            StartDate = evt.StartDate,
            EndDate = evt.EndDate ?? evt.StartDate,
            CreatedAt = evt.CreatedAt,
            ModifiedAt = evt.ModifiedAt,
            IsCompleted = evt.IsCompleted,
            Type = evt.EventType switch
            {
                StorageEventType.MedicationTreatment => DomainEventType.Medication,
                StorageEventType.MedicalVisit => DomainEventType.MedicalVisit,
                StorageEventType.Photo => DomainEventType.Photo,
                StorageEventType.Video => DomainEventType.Video,
                StorageEventType.CriticalWarning => DomainEventType.Warning,
                _ => DomainEventType.Recommendation
            }
        };
    }
    #endregion

    #region ICalendarService explicit implementation
    async Task<List<CalendarEvent>> ICalendarService.GetEventsForDateAsync(DateTime date)
        => (await GetEventsForDateAsync(date)).Select(MapToCalendarEvent).ToList();

    async Task<List<CalendarEvent>> ICalendarService.GetEventsForMonthAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        return (await GetEventsForRangeAsync(start, end)).Select(MapToCalendarEvent).ToList();
    }

    async Task<List<CalendarEvent>> ICalendarService.GetEventsForDateRangeAsync(DateTime startDate, DateTime endDate)
        => (await GetEventsForRangeAsync(startDate, endDate)).Select(MapToCalendarEvent).ToList();

    async Task<List<CalendarEvent>> ICalendarService.GetActiveRestrictionsAsync()
    {
        // For now, no separate restriction storage; return empty list
        return new List<CalendarEvent>();
    }

    async Task<List<CalendarEvent>> ICalendarService.GetPendingNotificationEventsAsync()
        => (await GetPendingEventsAsync()).Select(MapToCalendarEvent).ToList();

    async Task<List<CalendarEvent>> ICalendarService.GetOverdueEventsAsync()
    {
        var now = DateTime.UtcNow;
        var overdue = (await GetPendingEventsAsync())
                        .Where(e => e.StartDate < now && !e.IsCompleted);
        return overdue.Select(MapToCalendarEvent).ToList();
    }

    async Task<CalendarEvent> ICalendarService.GetEventByIdAsync(Guid id)
    {
        var evt = await GetEventByIdAsync(id);
        return evt == null ? null : MapToCalendarEvent(evt);
    }

    async Task<bool> ICalendarService.MarkEventAsCompletedAsync(Guid id)
        => await MarkEventAsCompletedAsync(id);

    async Task<bool> ICalendarService.UpdateEventAsync(CalendarEvent calendarEvent)
    {
        var updated = await UpdateEventAsync(MapToDomain(calendarEvent));
        return updated != null;
    }

    async Task<bool> ICalendarService.DeleteEventAsync(Guid id)
        => await DeleteEventAsync(id);

    async Task<CalendarEvent> ICalendarService.CreateEventAsync(CalendarEvent calendarEvent)
        => MapToCalendarEvent(await CreateEventAsync(MapToDomain(calendarEvent)));

    async Task<bool> ICalendarService.SynchronizeEventsAsync()
    {
        await UpdateLastSyncTimeAsync(DateTime.UtcNow);
        return true;
    }
    #endregion
}