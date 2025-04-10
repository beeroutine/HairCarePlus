using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Domain.Entities;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services;

/// <summary>
/// Service for managing hair transplant events
/// </summary>
public interface IHairTransplantEventService
{
    /// <summary>
    /// Gets events for a specific date
    /// </summary>
    Task<IEnumerable<HairTransplantEvent>> GetEventsForDateAsync(DateTime date);
    
    /// <summary>
    /// Gets events for a date range
    /// </summary>
    Task<IEnumerable<HairTransplantEvent>> GetEventsForRangeAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Gets a specific event by its ID
    /// </summary>
    Task<HairTransplantEvent> GetEventByIdAsync(Guid id);
    
    /// <summary>
    /// Creates a new event
    /// </summary>
    Task<HairTransplantEvent> CreateEventAsync(HairTransplantEvent @event);
    
    /// <summary>
    /// Updates an existing event
    /// </summary>
    Task<HairTransplantEvent> UpdateEventAsync(HairTransplantEvent @event);
    
    /// <summary>
    /// Deletes an event
    /// </summary>
    Task<bool> DeleteEventAsync(Guid id);
    
    /// <summary>
    /// Gets all pending events that haven't been completed
    /// </summary>
    Task<IEnumerable<HairTransplantEvent>> GetPendingEventsAsync();
    
    /// <summary>
    /// Marks an event as completed
    /// </summary>
    Task<bool> MarkEventAsCompletedAsync(Guid id);
    
    /// <summary>
    /// Gets the last synchronization time
    /// </summary>
    Task<DateTime> GetLastSyncTimeAsync();
    
    /// <summary>
    /// Updates the last synchronization time
    /// </summary>
    Task UpdateLastSyncTimeAsync(DateTime syncTime);
} 