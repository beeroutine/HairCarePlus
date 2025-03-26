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
            var events = new List<CalendarEvent>();
            
            // Today's events
            events.Add(new CalendarEvent
            {
                Id = 1,
                Title = "Утренний приём лекарств",
                Description = "Принять лекарство с завтраком",
                Date = today,
                EventType = EventType.Medication,
                TimeOfDay = TimeOfDay.Morning,
                ReminderTime = new TimeSpan(8, 0, 0),
                IsCompleted = false
            });
            
            events.Add(new CalendarEvent
            {
                Id = 2,
                Title = "Вечерний приём лекарств",
                Description = "Принять лекарство перед сном",
                Date = today,
                EventType = EventType.Medication,
                TimeOfDay = TimeOfDay.Evening,
                ReminderTime = new TimeSpan(20, 0, 0),
                IsCompleted = false
            });
            
            // Tomorrow's events
            events.Add(new CalendarEvent
            {
                Id = 3,
                Title = "Мытьё головы",
                Description = "Следовать назначенной процедуре мытья головы",
                Date = today.AddDays(1),
                EventType = EventType.Instruction,
                TimeOfDay = TimeOfDay.Morning,
                ReminderTime = new TimeSpan(9, 0, 0),
                IsCompleted = false
            });
            
            // Add more events across different days
            for (int i = 1; i <= 28; i++)
            {
                // Add medication events on even days
                if (i % 2 == 0)
                {
                    events.Add(new CalendarEvent
                    {
                        Id = 100 + i,
                        Title = "Приём лекарств",
                        Description = "Утренний приём лекарств",
                        Date = new DateTime(today.Year, today.Month, i),
                        EventType = EventType.Medication,
                        TimeOfDay = TimeOfDay.Morning,
                        IsCompleted = i < today.Day
                    });
                }
                
                // Add photo reports on days divisible by 5
                if (i % 5 == 0)
                {
                    events.Add(new CalendarEvent
                    {
                        Id = 200 + i,
                        Title = "Фотоотчёт",
                        Description = "Сделать фото головы со всех сторон",
                        Date = new DateTime(today.Year, today.Month, i),
                        EventType = EventType.Photo,
                        TimeOfDay = TimeOfDay.Afternoon,
                        IsCompleted = i < today.Day
                    });
                }
                
                // Add restrictions on days divisible by 7
                if (i % 7 == 0)
                {
                    events.Add(new CalendarEvent
                    {
                        Id = 300 + i,
                        Title = "Ограничение физической активности",
                        Description = "Избегать интенсивных физических нагрузок",
                        Date = new DateTime(today.Year, today.Month, i),
                        EventType = EventType.Restriction,
                        TimeOfDay = TimeOfDay.Morning,
                        ExpirationDate = new DateTime(today.Year, today.Month, i).AddDays(3),
                        IsCompleted = false
                    });
                }
                
                // Add instructions on days divisible by 3
                if (i % 3 == 0)
                {
                    events.Add(new CalendarEvent
                    {
                        Id = 400 + i,
                        Title = "Инструкция по уходу",
                        Description = "Специальный уход за кожей головы",
                        Date = new DateTime(today.Year, today.Month, i),
                        EventType = EventType.Instruction,
                        TimeOfDay = TimeOfDay.Evening,
                        IsCompleted = i < today.Day
                    });
                }
            }
            
            // Previous month events
            for (int i = 25; i <= 31; i++)
            {
                var previousMonthDate = currentMonth.AddMonths(-1).AddDays(i - 1);
                if (previousMonthDate.Month == currentMonth.AddMonths(-1).Month)
                {
                    events.Add(new CalendarEvent
                    {
                        Id = 500 + i,
                        Title = "Приём лекарств",
                        Description = "Ежедневный приём лекарств",
                        Date = previousMonthDate,
                        EventType = EventType.Medication,
                        TimeOfDay = TimeOfDay.Morning,
                        IsCompleted = true
                    });
                }
            }
            
            // Next month events
            for (int i = 1; i <= 10; i++)
            {
                events.Add(new CalendarEvent
                {
                    Id = 600 + i,
                    Title = $"Плановый осмотр {i}",
                    Description = "Плановый осмотр у врача",
                    Date = currentMonth.AddMonths(1).AddDays(i - 1),
                    EventType = i % 4 == 0 ? EventType.Instruction :
                                i % 3 == 0 ? EventType.Photo :
                                i % 2 == 0 ? EventType.Restriction : EventType.Medication,
                    TimeOfDay = i % 3 == 0 ? TimeOfDay.Morning :
                                i % 2 == 0 ? TimeOfDay.Afternoon : TimeOfDay.Evening,
                    IsCompleted = false
                });
            }
            
            return events;
        }

        #endregion
    }
} 