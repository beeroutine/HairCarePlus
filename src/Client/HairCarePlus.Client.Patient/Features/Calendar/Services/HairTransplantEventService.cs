using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Domain.Entities;
using HairCarePlus.Client.Patient.Features.Calendar.Domain.Repositories;
using HairCarePlus.Client.Patient.Infrastructure.Storage;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services;

public class HairTransplantEventService : IHairTransplantEventService
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
}