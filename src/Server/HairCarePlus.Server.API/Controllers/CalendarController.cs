using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HairCarePlus.Server.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CalendarController : ControllerBase
    {
        private static readonly List<CalendarEvent> _events = new List<CalendarEvent>
        {
            new CalendarEvent
            {
                Id = 1,
                Title = "Morning Medication",
                Description = "Take your morning medication with water",
                Date = DateTime.Now.Date,
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(8, 15, 0),
                EventType = EventType.Medication
            },
            new CalendarEvent
            {
                Id = 2,
                Title = "Evening Medication",
                Description = "Take your evening medication with food",
                Date = DateTime.Now.Date,
                StartTime = new TimeSpan(20, 0, 0),
                EndTime = new TimeSpan(20, 15, 0),
                EventType = EventType.Medication
            },
            new CalendarEvent
            {
                Id = 3,
                Title = "Dermatologist Appointment",
                Description = "Regular checkup with Dr. Smith",
                Date = DateTime.Now.Date.AddDays(2),
                StartTime = new TimeSpan(14, 0, 0),
                EndTime = new TimeSpan(15, 0, 0),
                EventType = EventType.Appointment
            },
            new CalendarEvent
            {
                Id = 4,
                Title = "No Washing Hair",
                Description = "Avoid washing your hair for 48 hours",
                Date = DateTime.Now.Date.AddDays(-1),
                ExpirationDate = DateTime.Now.Date.AddDays(1),
                EventType = EventType.Restriction
            }
        };

        [HttpGet("events")]
        public IActionResult GetEvents([FromQuery] DateTime? date, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            if (date.HasValue)
            {
                var eventsForDate = _events.Where(e => e.Date.Date == date.Value.Date).ToList();
                return Ok(eventsForDate);
            }
            else if (startDate.HasValue && endDate.HasValue)
            {
                var eventsInRange = _events
                    .Where(e => e.Date.Date >= startDate.Value.Date && e.Date.Date <= endDate.Value.Date)
                    .ToList();
                return Ok(eventsInRange);
            }
            
            return Ok(_events);
        }

        [HttpGet("events/pending-notifications")]
        public IActionResult GetPendingNotifications()
        {
            // Return events that are due today and haven't been completed yet
            var today = DateTime.Now.Date;
            var pendingEvents = _events
                .Where(e => e.Date.Date == today && !e.IsCompleted)
                .ToList();
            
            return Ok(pendingEvents);
        }

        [HttpGet("restrictions/active")]
        public IActionResult GetActiveRestrictions()
        {
            var now = DateTime.Now;
            var activeRestrictions = _events
                .Where(e => e.EventType == EventType.Restriction && 
                            (e.ExpirationDate == null || e.ExpirationDate >= now))
                .ToList();
            
            return Ok(activeRestrictions);
        }

        [HttpPut("events/{id}/complete")]
        public IActionResult MarkEventCompleted(int id, [FromBody] CompletionRequest request)
        {
            var eventToUpdate = _events.FirstOrDefault(e => e.Id == id);
            if (eventToUpdate == null)
            {
                return NotFound();
            }
            
            eventToUpdate.IsCompleted = request.IsCompleted;
            return Ok();
        }
    }

    public class CalendarEvent
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsCompleted { get; set; }
        public EventType EventType { get; set; }
        public DateTime? ExpirationDate { get; set; }
    }

    public enum EventType
    {
        Appointment = 0,
        Medication = 1,
        Treatment = 2,
        Exercise = 3,
        Restriction = 4,
        Other = 5
    }

    public class CompletionRequest
    {
        public bool IsCompleted { get; set; }
    }
} 