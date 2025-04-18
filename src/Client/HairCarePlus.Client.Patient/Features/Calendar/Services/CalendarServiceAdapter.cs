using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using DomainEntities = HairCarePlus.Client.Patient.Features.Calendar.Domain.Entities;
using HairCarePlus.Client.Patient.Features.Notifications.Services.Interfaces;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services;

/// <summary>
/// Адаптер для обеспечения совместимости между старым ICalendarService и новым IHairTransplantEventService
/// </summary>
public class CalendarServiceAdapter : ICalendarService
{
    private readonly IHairTransplantEventService _eventService;
    private readonly IRestrictionService _restrictionService;
    private readonly INotificationService _notificationService;

    public CalendarServiceAdapter(
        IHairTransplantEventService eventService,
        IRestrictionService restrictionService,
        INotificationService notificationService)
    {
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _restrictionService = restrictionService ?? throw new ArgumentNullException(nameof(restrictionService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    public async Task<List<CalendarEvent>> GetEventsForDateAsync(DateTime date)
    {
        var events = await _eventService.GetEventsForDateAsync(date);
        return events.Select(MapToCalendarEvent).ToList();
    }

    public async Task<List<CalendarEvent>> GetEventsForMonthAsync(int year, int month)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        var events = await _eventService.GetEventsForRangeAsync(startDate, endDate);
        return events.Select(MapToCalendarEvent).ToList();
    }

    public async Task<List<CalendarEvent>> GetEventsForDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var events = await _eventService.GetEventsForRangeAsync(startDate, endDate);
        return events.Select(MapToCalendarEvent).ToList();
    }

    public async Task<List<CalendarEvent>> GetActiveRestrictionsAsync()
    {
        var restrictions = await _restrictionService.GetActiveRestrictionsAsync();
        return restrictions.Select(MapToCalendarEvent).ToList();
    }

    public async Task<List<CalendarEvent>> GetPendingNotificationEventsAsync()
    {
        var events = await _eventService.GetPendingEventsAsync();
        return events.Select(MapToCalendarEvent).ToList();
    }

    public async Task<List<CalendarEvent>> GetOverdueEventsAsync()
    {
        var now = DateTime.UtcNow;
        var events = await _eventService.GetPendingEventsAsync();
        return events
            .Where(e => e.StartDate < now.Date && !e.IsCompleted)
            .Select(MapToCalendarEvent)
            .ToList();
    }

    public async Task<CalendarEvent> GetEventByIdAsync(Guid id)
    {
        var evt = await _eventService.GetEventByIdAsync(id);
        return evt != null ? MapToCalendarEvent(evt) : null;
    }

    public async Task<bool> MarkEventAsCompletedAsync(Guid id)
    {
        return await _eventService.MarkEventAsCompletedAsync(id);
    }

    public async Task<bool> UpdateEventAsync(CalendarEvent calendarEvent)
    {
        var domainEvent = MapToDomainEvent(calendarEvent);
        var result = await _eventService.UpdateEventAsync(domainEvent);
        return result != null;
    }

    public async Task<bool> DeleteEventAsync(Guid id)
    {
        return await _eventService.DeleteEventAsync(id);
    }

    public async Task<CalendarEvent> CreateEventAsync(CalendarEvent calendarEvent)
    {
        var domainEvent = MapToDomainEvent(calendarEvent);
        var result = await _eventService.CreateEventAsync(domainEvent);
        return MapToCalendarEvent(result);
    }

    public async Task<bool> SynchronizeEventsAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var startDate = now.AddDays(-30);
            var endDate = now.AddDays(30);
            
            // Get all events in range
            var events = await GetEventsForDateRangeAsync(startDate, endDate);
            
            // Update last sync time
            await _eventService.UpdateLastSyncTimeAsync(now);
            
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static CalendarEvent MapToCalendarEvent(DomainEntities.HairTransplantEvent e)
    {
        return new CalendarEvent
        {
            Id = e.Id,
            Title = e.Title,
            Description = e.Notes ?? string.Empty,
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            IsCompleted = e.IsCompleted,
            EventType = MapEventType(e.Type),
            Priority = GetEventPriority(e.Type),
            TimeOfDay = GetTimeOfDay(e.StartDate),
            ReminderTime = e.EndDate - e.StartDate,
            CreatedAt = e.CreatedAt,
            ModifiedAt = e.ModifiedAt
        };
    }

    private static DomainEntities.HairTransplantEvent MapToDomainEvent(CalendarEvent e)
    {
        return new DomainEntities.HairTransplantEvent
        {
            Id = e.Id,
            Title = e.Title,
            Notes = e.Description,
            StartDate = e.StartDate,
            EndDate = e.EndDate ?? e.StartDate,
            IsCompleted = e.IsCompleted,
            Type = MapToDomainEventType(e.EventType),
            CreatedAt = e.CreatedAt,
            ModifiedAt = e.ModifiedAt
        };
    }

    private static EventType MapEventType(DomainEntities.EventType type)
    {
        return type switch
        {
            DomainEntities.EventType.Medication => EventType.MedicationTreatment,
            DomainEntities.EventType.MedicalVisit => EventType.MedicalVisit,
            DomainEntities.EventType.Photo => EventType.Photo,
            DomainEntities.EventType.Video => EventType.Video,
            DomainEntities.EventType.Recommendation => EventType.GeneralRecommendation,
            DomainEntities.EventType.Warning => EventType.CriticalWarning,
            _ => EventType.GeneralRecommendation
        };
    }

    private static DomainEntities.EventType MapToDomainEventType(EventType type)
    {
        return type switch
        {
            EventType.MedicationTreatment => DomainEntities.EventType.Medication,
            EventType.MedicalVisit => DomainEntities.EventType.MedicalVisit,
            EventType.Photo => DomainEntities.EventType.Photo,
            EventType.Video => DomainEntities.EventType.Video,
            EventType.GeneralRecommendation => DomainEntities.EventType.Recommendation,
            EventType.CriticalWarning => DomainEntities.EventType.Warning,
            _ => DomainEntities.EventType.Recommendation
        };
    }

    private static EventPriority GetEventPriority(DomainEntities.EventType type)
    {
        return type switch
        {
            DomainEntities.EventType.Warning => EventPriority.Critical,
            DomainEntities.EventType.Medication => EventPriority.High,
            DomainEntities.EventType.MedicalVisit => EventPriority.High,
            DomainEntities.EventType.Photo => EventPriority.Normal,
            DomainEntities.EventType.Video => EventPriority.Normal,
            DomainEntities.EventType.Recommendation => EventPriority.Low,
            _ => EventPriority.Normal
        };
    }

    private static TimeOfDay GetTimeOfDay(DateTime date)
    {
        var hour = date.Hour;
        return hour switch
        {
            < 12 => TimeOfDay.Morning,
            < 17 => TimeOfDay.Afternoon,
            _ => TimeOfDay.Evening
        };
    }
} 