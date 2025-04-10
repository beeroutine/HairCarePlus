using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Domain.Entities;

namespace HairCarePlus.Client.Patient.Features.Calendar.Domain.Repositories;

public interface ICalendarRepository
{
    Task<IEnumerable<HairTransplantEvent>> GetEventsForDateAsync(DateTime date);
    Task<IEnumerable<HairTransplantEvent>> GetEventsForRangeAsync(DateTime startDate, DateTime endDate);
    Task<HairTransplantEvent> GetEventByIdAsync(Guid id);
    Task<HairTransplantEvent> AddEventAsync(HairTransplantEvent @event);
    Task<HairTransplantEvent> UpdateEventAsync(HairTransplantEvent @event);
    Task<bool> DeleteEventAsync(Guid id);
    Task<IEnumerable<HairTransplantEvent>> GetPendingEventsAsync();
    Task<bool> MarkEventAsCompletedAsync(Guid id);
    Task<DateTime> GetLastSyncTimeAsync();
    Task UpdateLastSyncTimeAsync(DateTime syncTime);
} 