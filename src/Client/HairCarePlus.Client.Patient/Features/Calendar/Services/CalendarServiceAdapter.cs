using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Domain.Entities;
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

    public async Task<IEnumerable<CalendarEvent>> GetEventsForDateAsync(DateTime date)
    {
        var events = await _eventService.GetEventsForDateAsync(date);
        return events.Select(MapToCalendarEvent);
    }

    public async Task<IEnumerable<CalendarEvent>> GetEventsForMonthAsync(int year, int month)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        var events = await _eventService.GetEventsForRangeAsync(startDate, endDate);
        return events.Select(MapToCalendarEvent);
    }

    public async Task<IEnumerable<CalendarEvent>> GetEventsForDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var events = await _eventService.GetEventsForRangeAsync(startDate, endDate);
        return events.Select(MapToCalendarEvent);
    }

    public async Task MarkEventAsCompletedAsync(int eventId, bool isCompleted)
    {
        // Примечание: здесь мы конвертируем int в Guid. В реальном приложении нужно
        // реализовать правильное маппирование ID или обновить всю систему на использование Guid
        var guid = new Guid(eventId.ToString().PadLeft(32, '0'));
        if (isCompleted)
        {
            await _eventService.MarkEventAsCompletedAsync(guid);
        }
        else
        {
            var existingEvent = await _eventService.GetEventByIdAsync(guid);
            if (existingEvent != null)
            {
                existingEvent.IsCompleted = false;
                await _eventService.UpdateEventAsync(existingEvent);
            }
        }
    }

    public async Task<IEnumerable<CalendarEvent>> GetActiveRestrictionsAsync()
    {
        var restrictions = await _restrictionService.GetActiveRestrictionsAsync();
        return restrictions.Select(MapToCalendarEvent);
    }

    public async Task<IEnumerable<CalendarEvent>> GetPendingNotificationEventsAsync()
    {
        var events = await _eventService.GetPendingEventsAsync();
        return events.Where(e => !e.IsNotified).Select(MapToCalendarEvent);
    }

    public async Task<IEnumerable<CalendarEvent>> GetOverdueEventsAsync()
    {
        var events = await _eventService.GetPendingEventsAsync();
        return events
            .Where(e => e.StartDate < DateTime.Today && !e.IsCompleted)
            .Select(MapToCalendarEvent);
    }

    private static CalendarEvent MapToCalendarEvent(HairTransplantEvent e)
    {
        return new CalendarEvent
        {
            Id = int.Parse(e.Id.ToString().Substring(0, 8), System.Globalization.NumberStyles.HexNumber), // Временное решение для конвертации Guid в int
            Title = e.Title,
            Description = e.Description,
            Date = e.StartDate,
            EndDate = e.EndDate,
            IsCompleted = e.IsCompleted,
            EventType = MapEventType(e.Type),
            Priority = MapEventPriority(e.Priority),
            TimeOfDay = GetTimeOfDay(e.StartDate),
            ReminderTime = e.Duration,
            ExpirationDate = e.EndDate
        };
    }

    private static HairCarePlus.Client.Patient.Features.Calendar.Models.EventType MapEventType(Domain.Entities.EventType type)
    {
        return type switch
        {
            Domain.Entities.EventType.Medication => HairCarePlus.Client.Patient.Features.Calendar.Models.EventType.MedicationTreatment,
            Domain.Entities.EventType.Checkup => HairCarePlus.Client.Patient.Features.Calendar.Models.EventType.MedicalVisit,
            Domain.Entities.EventType.Washing => HairCarePlus.Client.Patient.Features.Calendar.Models.EventType.GeneralRecommendation,
            Domain.Entities.EventType.Exercise => HairCarePlus.Client.Patient.Features.Calendar.Models.EventType.GeneralRecommendation,
            Domain.Entities.EventType.Photo => HairCarePlus.Client.Patient.Features.Calendar.Models.EventType.Photo,
            Domain.Entities.EventType.Restriction => HairCarePlus.Client.Patient.Features.Calendar.Models.EventType.CriticalWarning,
            _ => HairCarePlus.Client.Patient.Features.Calendar.Models.EventType.GeneralRecommendation
        };
    }

    private static HairCarePlus.Client.Patient.Features.Calendar.Models.EventPriority MapEventPriority(Domain.Entities.EventPriority priority)
    {
        return priority switch
        {
            Domain.Entities.EventPriority.Low => HairCarePlus.Client.Patient.Features.Calendar.Models.EventPriority.Low,
            Domain.Entities.EventPriority.Normal => HairCarePlus.Client.Patient.Features.Calendar.Models.EventPriority.Normal,
            Domain.Entities.EventPriority.High => HairCarePlus.Client.Patient.Features.Calendar.Models.EventPriority.High,
            Domain.Entities.EventPriority.Critical => HairCarePlus.Client.Patient.Features.Calendar.Models.EventPriority.Critical,
            _ => HairCarePlus.Client.Patient.Features.Calendar.Models.EventPriority.Normal
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