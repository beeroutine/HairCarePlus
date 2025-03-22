using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Infrastructure.Services;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services
{
    public class CalendarService : ICalendarService
    {
        private readonly HttpClient _httpClient;
        private readonly INetworkService _networkService;
        private readonly string _baseApiUrl = "api/calendar";
        private readonly bool _useMockData = true; // Set to true to use mock data instead of API

        public CalendarService(HttpClient httpClient, INetworkService networkService)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
            
            // Set base address if not already set
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri("http://localhost:5281/");
            }
        }

        public async Task<IEnumerable<CalendarEvent>> GetEventsForDateAsync(DateTime date)
        {
            try
            {
                // Check for network connectivity
                if (!await _networkService.IsConnectedAsync() || _useMockData)
                {
                    // Return mock data
                    return GetMockEventsForDate(date);
                }
                
                string apiUrl = $"{_baseApiUrl}/events?date={date:yyyy-MM-dd}";
                var events = await _httpClient.GetFromJsonAsync<List<CalendarEvent>>(apiUrl);
                return events ?? new List<CalendarEvent>();
            }
            catch (Exception ex)
            {
                // Log the exception
                System.Diagnostics.Debug.WriteLine($"Error in GetEventsForDateAsync: {ex.Message}");
                // Return mock data on error
                return GetMockEventsForDate(date);
            }
        }

        public async Task<IEnumerable<CalendarEvent>> GetEventsForDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                // Check for network connectivity
                if (!await _networkService.IsConnectedAsync() || _useMockData)
                {
                    // Return mock data
                    return GetMockEventsForDateRange(startDate, endDate);
                }
                
                string apiUrl = $"{_baseApiUrl}/events?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";
                var events = await _httpClient.GetFromJsonAsync<List<CalendarEvent>>(apiUrl);
                return events ?? new List<CalendarEvent>();
            }
            catch (Exception ex)
            {
                // Log the exception
                System.Diagnostics.Debug.WriteLine($"Error in GetEventsForDateRangeAsync: {ex.Message}");
                // Return mock data on error
                return GetMockEventsForDateRange(startDate, endDate);
            }
        }

        public async Task MarkEventAsCompletedAsync(int eventId, bool isCompleted)
        {
            try
            {
                // Check for network connectivity
                if (!await _networkService.IsConnectedAsync() || _useMockData)
                {
                    // Simulate a successful update
                    return;
                }
                
                string apiUrl = $"{_baseApiUrl}/events/{eventId}/complete";
                var response = await _httpClient.PutAsJsonAsync(apiUrl, new { isCompleted });
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                // Log the exception
                System.Diagnostics.Debug.WriteLine($"Error in MarkEventAsCompletedAsync: {ex.Message}");
                // Just return, no need to throw exception in a demo
            }
        }

        public async Task<IEnumerable<CalendarEvent>> GetActiveRestrictionsAsync()
        {
            try
            {
                // Check for network connectivity
                if (!await _networkService.IsConnectedAsync() || _useMockData)
                {
                    // Return mock restrictions
                    return GetMockActiveRestrictions();
                }
                
                string apiUrl = $"{_baseApiUrl}/restrictions/active";
                var restrictions = await _httpClient.GetFromJsonAsync<List<CalendarEvent>>(apiUrl);
                return restrictions?
                    .Where(r => r.EventType == EventType.Restriction && 
                                (r.ExpirationDate == null || r.ExpirationDate > DateTime.Now))
                    .ToList() ?? new List<CalendarEvent>();
            }
            catch (Exception ex)
            {
                // Log the exception
                System.Diagnostics.Debug.WriteLine($"Error in GetActiveRestrictionsAsync: {ex.Message}");
                // Return mock data on error
                return GetMockActiveRestrictions();
            }
        }

        public async Task<IEnumerable<CalendarEvent>> GetPendingNotificationEventsAsync()
        {
            try
            {
                // Check for network connectivity
                if (!await _networkService.IsConnectedAsync() || _useMockData)
                {
                    // Return mock pending notifications
                    return GetMockPendingNotifications();
                }
                
                string apiUrl = $"{_baseApiUrl}/events/pending-notifications";
                var events = await _httpClient.GetFromJsonAsync<List<CalendarEvent>>(apiUrl);
                return events ?? new List<CalendarEvent>();
            }
            catch (Exception ex)
            {
                // Log the exception
                System.Diagnostics.Debug.WriteLine($"Error in GetPendingNotificationEventsAsync: {ex.Message}");
                // Return mock data on error
                return GetMockPendingNotifications();
            }
        }

        public async Task<IEnumerable<CalendarEvent>> GetEventsForMonthAsync(int year, int month)
        {
            try
            {
                // Get the first and last day of the month
                var firstDayOfMonth = new DateTime(year, month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
                
                // We also need to include the days from previous/next month that appear in the calendar view
                var firstDayOfCalendarView = firstDayOfMonth.AddDays(-(int)firstDayOfMonth.DayOfWeek);
                var lastDayOfCalendarView = lastDayOfMonth.AddDays(6 - (int)lastDayOfMonth.DayOfWeek);
                
                // Get events for the entire calendar view
                var events = await GetEventsForDateRangeAsync(firstDayOfCalendarView, lastDayOfCalendarView);
                return events;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetEventsForMonthAsync: {ex.Message}");
                return new List<CalendarEvent>();
            }
        }

        #region Mock Data Methods

        private List<CalendarEvent> GetMockEventsForDate(DateTime date)
        {
            // Get events from the mock data that match the requested date
            return GetAllMockEvents()
                .Where(e => e.Date.Date == date.Date)
                .ToList();
        }

        private List<CalendarEvent> GetMockEventsForDateRange(DateTime startDate, DateTime endDate)
        {
            // Get events from the mock data that fall within the date range
            return GetAllMockEvents()
                .Where(e => e.Date.Date >= startDate.Date && e.Date.Date <= endDate.Date)
                .ToList();
        }

        private List<CalendarEvent> GetMockActiveRestrictions()
        {
            return GetAllMockEvents()
                .Where(e => e.EventType == EventType.Restriction && 
                          (e.ExpirationDate == null || e.ExpirationDate > DateTime.Now))
                .ToList();
        }

        private List<CalendarEvent> GetMockPendingNotifications()
        {
            return GetAllMockEvents()
                .Where(e => e.Date.Date == DateTime.Today && !e.IsCompleted)
                .ToList();
        }

        private List<CalendarEvent> GetAllMockEvents()
        {
            // Current date for reference
            var today = DateTime.Today;
            var currentMonth = new DateTime(today.Year, today.Month, 1);
            
            // Create a list of mock calendar events
            return new List<CalendarEvent>
            {
                // Today's events
                new CalendarEvent
                {
                    Id = 1,
                    Title = "Morning Medication",
                    Description = "Take your morning medication with breakfast",
                    Date = today,
                    EventType = EventType.Medication,
                    ReminderTime = new TimeSpan(8, 0, 0),
                    IsCompleted = false
                },
                new CalendarEvent
                {
                    Id = 2,
                    Title = "Evening Medication",
                    Description = "Take your evening medication before bed",
                    Date = today,
                    EventType = EventType.Medication,
                    ReminderTime = new TimeSpan(20, 0, 0),
                    IsCompleted = false
                },
                
                // Tomorrow's events
                new CalendarEvent
                {
                    Id = 3,
                    Title = "Hair Washing",
                    Description = "Wash your hair following the prescribed procedure",
                    Date = today.AddDays(1),
                    EventType = EventType.Instruction,
                    ReminderTime = new TimeSpan(9, 0, 0),
                    IsCompleted = false
                },
                
                // Next week events
                new CalendarEvent
                {
                    Id = 4,
                    Title = "Photo Report",
                    Description = "Take photos of your scalp from all angles",
                    Date = today.AddDays(7),
                    EventType = EventType.Photo,
                    ReminderTime = new TimeSpan(12, 0, 0),
                    IsCompleted = false
                },
                new CalendarEvent
                {
                    Id = 5,
                    Title = "Doctor Appointment",
                    Description = "Visit Dr. Smith for follow-up",
                    Date = today.AddDays(10),
                    EventType = EventType.Medication,
                    ReminderTime = new TimeSpan(14, 30, 0),
                    IsCompleted = false
                },
                
                // Restrictions
                new CalendarEvent
                {
                    Id = 6,
                    Title = "No Hair Washing",
                    Description = "Avoid washing your hair for 3 days",
                    Date = today.AddDays(-1),
                    ExpirationDate = today.AddDays(2),
                    EventType = EventType.Restriction,
                    IsCompleted = false
                },
                new CalendarEvent
                {
                    Id = 7,
                    Title = "No Physical Activity",
                    Description = "Avoid strenuous physical activity for 7 days",
                    Date = today.AddDays(-2),
                    ExpirationDate = today.AddDays(5),
                    EventType = EventType.Restriction,
                    IsCompleted = false
                },
                
                // Past events
                new CalendarEvent
                {
                    Id = 8,
                    Title = "Morning Medication",
                    Description = "Take your morning medication with breakfast",
                    Date = today.AddDays(-3),
                    EventType = EventType.Medication,
                    IsCompleted = true
                },
                new CalendarEvent
                {
                    Id = 9,
                    Title = "Apply Ointment",
                    Description = "Apply prescribed ointment to affected areas",
                    Date = today.AddDays(-2),
                    EventType = EventType.Medication,
                    IsCompleted = true
                },
                
                // Next month's events
                new CalendarEvent
                {
                    Id = 10,
                    Title = "Follow-up Visit",
                    Description = "Hospital follow-up appointment",
                    Date = currentMonth.AddMonths(1).AddDays(15),
                    EventType = EventType.Medication,
                    IsCompleted = false
                }
            };
        }

        #endregion
    }
} 