using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services.Implementation
{
    public class CalendarServiceImpl : ICalendarService
    {
        private readonly IEventAggregationService _eventAggregationService;
        private readonly IHairTransplantEventGenerator _eventGenerator;

        public CalendarServiceImpl(
            IEventAggregationService eventAggregationService,
            IHairTransplantEventGenerator eventGenerator)
        {
            _eventAggregationService = eventAggregationService;
            _eventGenerator = eventGenerator;
        }

        public async Task<List<CalendarEvent>> GetEventsForDateAsync(DateTime date)
        {
            var events = await _eventAggregationService.GetEventsForDateAsync(date);
            return events.ToList();
        }

        public async Task<List<CalendarEvent>> GetEventsForMonthAsync(int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            return await GetEventsForDateRangeAsync(startDate, endDate);
        }

        public async Task<List<CalendarEvent>> GetEventsForDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var events = await _eventAggregationService.GetEventsForDateRangeAsync(startDate, endDate);
            return events.ToList();
        }

        public async Task<List<CalendarEvent>> GetActiveRestrictionsAsync()
        {
            var events = await _eventAggregationService.GetActiveRestrictionsAsync();
            return events.ToList();
        }

        public async Task<List<CalendarEvent>> GetPendingNotificationEventsAsync()
        {
            var now = DateTime.UtcNow;
            var events = await _eventAggregationService.GetEventsForDateRangeAsync(now, now.AddDays(7));
            return events.Where(e => 
                !e.IsCompleted && 
                e.StartDate > now && 
                e.ReminderTime > TimeSpan.Zero
            ).ToList();
        }

        public async Task<List<CalendarEvent>> GetOverdueEventsAsync()
        {
            var now = DateTime.UtcNow;
            var events = await _eventAggregationService.GetEventsForDateRangeAsync(now.AddDays(-30), now);
            return events.Where(e => !e.IsCompleted && e.StartDate < now).ToList();
        }

        public async Task<CalendarEvent> GetEventByIdAsync(int eventId)
        {
            return await _eventAggregationService.GetEventByIdAsync(eventId);
        }

        public async Task<bool> MarkEventAsCompletedAsync(int eventId)
        {
            var calendarEvent = await GetEventByIdAsync(eventId);
            if (calendarEvent == null) return false;

            calendarEvent.IsCompleted = true;
            calendarEvent.ModifiedAt = DateTime.UtcNow;

            return await UpdateEventAsync(calendarEvent);
        }

        public async Task<bool> UpdateEventAsync(CalendarEvent calendarEvent)
        {
            calendarEvent.ModifiedAt = DateTime.UtcNow;
            return await _eventAggregationService.UpdateEventAsync(calendarEvent);
        }

        public async Task<bool> DeleteEventAsync(int eventId)
        {
            return await _eventAggregationService.DeleteEventAsync(eventId);
        }

        public async Task<CalendarEvent> CreateEventAsync(CalendarEvent calendarEvent)
        {
            calendarEvent.CreatedAt = DateTime.UtcNow;
            calendarEvent.ModifiedAt = DateTime.UtcNow;
            return await _eventAggregationService.CreateEventAsync(calendarEvent);
        }

        public async Task<bool> SynchronizeEventsAsync()
        {
            try
            {
                // Get all events from the last 30 days and upcoming 30 days
                var now = DateTime.UtcNow;
                var startDate = now.AddDays(-30);
                var endDate = now.AddDays(30);
                
                var events = await GetEventsForDateRangeAsync(startDate, endDate);
                
                // TODO: Implement actual synchronization with the server
                // For now, just return true to indicate success
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
} 