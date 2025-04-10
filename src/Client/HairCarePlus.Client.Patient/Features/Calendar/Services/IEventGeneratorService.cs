using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Domain.Entities;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services;

/// <summary>
/// Service for generating hair transplant events based on the transplant date
/// </summary>
public interface IEventGeneratorService
{
    /// <summary>
    /// Sets the transplant date for event generation
    /// </summary>
    Task SetTransplantDateAsync(DateTime transplantDate);
    
    /// <summary>
    /// Gets the current transplant date
    /// </summary>
    Task<DateTime> GetTransplantDateAsync();
    
    /// <summary>
    /// Generates events for a specific date based on the transplant timeline
    /// </summary>
    Task<IEnumerable<HairTransplantEvent>> GenerateEventsForDateAsync(DateTime date);
    
    /// <summary>
    /// Generates events for a date range based on the transplant timeline
    /// </summary>
    Task<IEnumerable<HairTransplantEvent>> GenerateEventsForRangeAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Generates the initial set of events after transplant
    /// </summary>
    Task<IEnumerable<HairTransplantEvent>> GenerateInitialEventsAsync();
    
    /// <summary>
    /// Gets the current recovery phase based on days since transplant
    /// </summary>
    Task<int> GetCurrentPhaseAsync();
} 