using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using HairCarePlus.Client.Patient.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services.Implementations
{
    public class CalendarService : ICalendarService
    {
        private readonly AppDbContext _dbContext;

        public CalendarService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<CalendarEvent>> GetEventsForDateAsync(DateTime date)
        {
            return await _dbContext.Events
                .Where(e => e.StartDate.Date == date.Date)
                .OrderBy(e => e.StartDate)
                .ToListAsync();
        }

        public async Task<List<CalendarEvent>> GetEventsForMonthAsync(int year, int month)
        {
            var startOfMonth = new DateTime(year, month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
            
            return await GetEventsForDateRangeAsync(startOfMonth, endOfMonth);
        }

        public async Task<List<CalendarEvent>> GetEventsForDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbContext.Events
                .Where(e => e.StartDate.Date >= startDate.Date && e.StartDate.Date <= endDate.Date)
                .OrderBy(e => e.StartDate)
                .ToListAsync();
        }

        public async Task<List<CalendarEvent>> GetActiveRestrictionsAsync()
        {
            var now = DateTime.UtcNow;
            return await _dbContext.Events
                .Where(e => e.EventType == EventType.CriticalWarning && 
                          (!e.EndDate.HasValue || e.EndDate.Value > now))
                .OrderBy(e => e.StartDate)
                .ToListAsync();
        }

        public async Task<List<CalendarEvent>> GetPendingNotificationEventsAsync()
        {
            var now = DateTime.UtcNow;
            return await _dbContext.Events
                .Where(e => !e.IsCompleted && e.StartDate > now && e.ReminderTime > TimeSpan.Zero)
                .OrderBy(e => e.StartDate)
                .ToListAsync();
        }

        public async Task<List<CalendarEvent>> GetOverdueEventsAsync()
        {
            var now = DateTime.UtcNow;
            return await _dbContext.Events
                .Where(e => !e.IsCompleted && e.StartDate < now)
                .OrderBy(e => e.StartDate)
                .ToListAsync();
        }

        public async Task<CalendarEvent> GetEventByIdAsync(int eventId)
        {
            return await _dbContext.Events.FindAsync(eventId);
        }

        public async Task<bool> MarkEventAsCompletedAsync(int eventId)
        {
            var calendarEvent = await _dbContext.Events.FindAsync(eventId);
            if (calendarEvent == null) return false;

            calendarEvent.IsCompleted = true;
            calendarEvent.ModifiedAt = DateTime.UtcNow;
            
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateEventAsync(CalendarEvent calendarEvent)
        {
            _dbContext.Events.Update(calendarEvent);
            var saveResult = await _dbContext.SaveChangesAsync();
            return saveResult > 0;
        }

        public async Task<bool> DeleteEventAsync(int eventId)
        {
            var calendarEvent = await _dbContext.Events.FindAsync(eventId);
            if (calendarEvent == null) return false;

            _dbContext.Events.Remove(calendarEvent);
            var saveResult = await _dbContext.SaveChangesAsync();
            return saveResult > 0;
        }

        public async Task<CalendarEvent> CreateEventAsync(CalendarEvent calendarEvent)
        {
            calendarEvent.CreatedAt = DateTime.UtcNow;
            calendarEvent.ModifiedAt = DateTime.UtcNow;
            _dbContext.Events.Add(calendarEvent);
            await _dbContext.SaveChangesAsync();
            return calendarEvent;
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