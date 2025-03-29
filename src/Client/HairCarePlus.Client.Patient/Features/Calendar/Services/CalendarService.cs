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
        private readonly IHairTransplantEventGenerator _eventGenerator;
        private readonly string _baseApiUrl = "api/calendar";
        private readonly bool _useMockData = true; // Set to true to use mock data instead of API

        public CalendarService(HttpClient httpClient, INetworkService networkService, IHairTransplantEventGenerator eventGenerator)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
            _eventGenerator = eventGenerator ?? throw new ArgumentNullException(nameof(eventGenerator));
            
            // Set base address if not already set
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri("http://localhost:5281/");
            }
            
            // Устанавливаем демо-дату трансплантации волос для генерации событий
            _eventGenerator.SetTransplantDate(DateTime.Today.AddDays(-1));
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
                    .Where(r => r.EventType == EventType.CriticalWarning && 
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
        
        public async Task<IEnumerable<CalendarEvent>> GetOverdueEventsAsync()
        {
            try
            {
                // Check for network connectivity
                if (!await _networkService.IsConnectedAsync() || _useMockData)
                {
                    // Return mock overdue events
                    return GetMockOverdueEvents();
                }
                
                string apiUrl = $"{_baseApiUrl}/events/overdue";
                var events = await _httpClient.GetFromJsonAsync<List<CalendarEvent>>(apiUrl);
                return events ?? new List<CalendarEvent>();
            }
            catch (Exception ex)
            {
                // Log the exception
                System.Diagnostics.Debug.WriteLine($"Error in GetOverdueEventsAsync: {ex.Message}");
                // Return mock data on error
                return GetMockOverdueEvents();
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

        private IEnumerable<CalendarEvent> GetMockEventsForDate(DateTime date)
        {
            // Используем новый генератор событий вместо прежней логики
            return _eventGenerator.GenerateEventsForDate(date);
        }
        
        private IEnumerable<CalendarEvent> GetMockEventsForDateRange(DateTime startDate, DateTime endDate)
        {
            // Используем новый генератор событий вместо прежней логики
            return _eventGenerator.GenerateEventsForDateRange(startDate, endDate);
        }

        private List<CalendarEvent> GetMockActiveRestrictions()
        {
            // Получить ограничения из всех событий за последние 90 дней
            var allEvents = _eventGenerator.GenerateEventsForDateRange(DateTime.Today.AddDays(-90), DateTime.Today.AddDays(30));
            
            // Выбрать только активные ограничения (события типа CriticalWarning, которые ещё не истекли)
            return allEvents
                .Where(e => e.EventType == EventType.CriticalWarning && 
                           (e.EndDate == null || e.EndDate >= DateTime.Now))
                .ToList();
        }
        
        private List<CalendarEvent> GetMockPendingNotifications()
        {
            // Получить события на ближайшие 2 дня для уведомлений
            var events = _eventGenerator.GenerateEventsForDateRange(DateTime.Today, DateTime.Today.AddDays(2));
            
            // Выбрать события с высоким приоритетом для уведомлений
            return events
                .Where(e => e.Priority == EventPriority.High || e.Priority == EventPriority.Critical)
                .ToList();
        }
        
        private List<CalendarEvent> GetMockOverdueEvents()
        {
            // Получить события за последние 10 дней
            var events = _eventGenerator.GenerateEventsForDateRange(DateTime.Today.AddDays(-10), DateTime.Today.AddDays(-1));
            
            // Выбрать невыполненные события
            return events
                .Where(e => !e.IsCompleted)
                .ToList();
        }
        
        private List<CalendarEvent> GetAllMockEvents()
        {
            // Получить события за весь период (последние 90 дней + ближайшие 90 дней)
            return _eventGenerator.GenerateEventsForDateRange(
                DateTime.Today.AddDays(-90), 
                DateTime.Today.AddDays(90)).ToList();
        }
        
        #endregion
    }
} 