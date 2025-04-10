using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services
{
    public class EventAggregationService : IEventAggregationService
    {
        private readonly ICalendarService _calendarService;

        public EventAggregationService(ICalendarService calendarService)
        {
            _calendarService = calendarService;
        }

        public async Task<List<CalendarEvent>> GetAllEventsForDateAsync(DateTime date)
        {
            var allEvents = new List<CalendarEvent>();
            
            try
            {
                // Get regular calendar events
                var events = await _calendarService.GetEventsForDateAsync(date);
                if (events != null)
                {
                    allEvents.AddRange(events);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading calendar events: {ex.Message}");
                // Continue execution to try loading restrictions
            }
            
            try
            {
                // Get active restrictions
                var restrictions = await _calendarService.GetActiveRestrictionsAsync();
                if (restrictions != null)
                {
                    // Filter restrictions that are active on the specified date
                    var activeRestrictions = restrictions
                        .Where(r => r.Date <= date && 
                                   (r.ExpirationDate == null || r.ExpirationDate >= date))
                        .ToList();
                        
                    // Add restrictions that aren't already in allEvents
                    foreach (var restriction in activeRestrictions)
                    {
                        if (!allEvents.Any(e => e.Id == restriction.Id))
                        {
                            allEvents.Add(restriction);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading restrictions: {ex.Message}");
            }
            
            return allEvents;
        }
    }
} 