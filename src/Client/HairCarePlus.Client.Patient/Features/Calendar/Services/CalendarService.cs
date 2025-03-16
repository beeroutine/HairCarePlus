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

        public CalendarService(HttpClient httpClient, INetworkService networkService)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
            
            // Set base address if not already set
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri("https://api.haircareplus.com/");
            }
        }

        public async Task<IEnumerable<CalendarEvent>> GetEventsForDateAsync(DateTime date)
        {
            try
            {
                // Check for network connectivity
                if (!await _networkService.IsConnectedAsync())
                {
                    // Return cached data or empty list
                    return new List<CalendarEvent>();
                }
                
                string apiUrl = $"{_baseApiUrl}/events?date={date:yyyy-MM-dd}";
                var events = await _httpClient.GetFromJsonAsync<List<CalendarEvent>>(apiUrl);
                return events ?? new List<CalendarEvent>();
            }
            catch (Exception)
            {
                // In a real app, log the exception
                return new List<CalendarEvent>();
            }
        }

        public async Task<IEnumerable<CalendarEvent>> GetEventsForDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                // Check for network connectivity
                if (!await _networkService.IsConnectedAsync())
                {
                    // Return cached data or empty list
                    return new List<CalendarEvent>();
                }
                
                string apiUrl = $"{_baseApiUrl}/events?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";
                var events = await _httpClient.GetFromJsonAsync<List<CalendarEvent>>(apiUrl);
                return events ?? new List<CalendarEvent>();
            }
            catch (Exception)
            {
                // In a real app, log the exception
                return new List<CalendarEvent>();
            }
        }

        public async Task MarkEventAsCompletedAsync(int eventId, bool isCompleted)
        {
            try
            {
                // Check for network connectivity
                if (!await _networkService.IsConnectedAsync())
                {
                    // Cache the operation for later execution
                    throw new InvalidOperationException("No network connectivity");
                }
                
                string apiUrl = $"{_baseApiUrl}/events/{eventId}/complete";
                var response = await _httpClient.PutAsJsonAsync(apiUrl, new { isCompleted });
                response.EnsureSuccessStatusCode();
            }
            catch (Exception)
            {
                // In a real app, log the exception
                throw;
            }
        }

        public async Task<IEnumerable<CalendarEvent>> GetActiveRestrictionsAsync()
        {
            try
            {
                // Check for network connectivity
                if (!await _networkService.IsConnectedAsync())
                {
                    // Return cached data or empty list
                    return new List<CalendarEvent>();
                }
                
                string apiUrl = $"{_baseApiUrl}/restrictions/active";
                var restrictions = await _httpClient.GetFromJsonAsync<List<CalendarEvent>>(apiUrl);
                return restrictions?
                    .Where(r => r.EventType == EventType.Restriction && 
                                (r.ExpirationDate == null || r.ExpirationDate > DateTime.Now))
                    .ToList() ?? new List<CalendarEvent>();
            }
            catch (Exception)
            {
                // In a real app, log the exception
                return new List<CalendarEvent>();
            }
        }

        public async Task<IEnumerable<CalendarEvent>> GetPendingNotificationEventsAsync()
        {
            try
            {
                // Check for network connectivity
                if (!await _networkService.IsConnectedAsync())
                {
                    // Return cached data or empty list
                    return new List<CalendarEvent>();
                }
                
                string apiUrl = $"{_baseApiUrl}/events/pending-notifications";
                var events = await _httpClient.GetFromJsonAsync<List<CalendarEvent>>(apiUrl);
                return events ?? new List<CalendarEvent>();
            }
            catch (Exception)
            {
                // In a real app, log the exception
                return new List<CalendarEvent>();
            }
        }
    }
} 