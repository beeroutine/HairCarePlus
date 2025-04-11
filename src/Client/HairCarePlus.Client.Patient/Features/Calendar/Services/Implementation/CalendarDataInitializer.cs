using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using HairCarePlus.Client.Patient.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services.Implementation
{
    public class CalendarDataInitializer : IDataInitializer
    {
        private readonly AppDbContext _dbContext;
        private readonly IHairTransplantEventGenerator _eventGenerator;
        private readonly ILocalStorageService _localStorage;
        private readonly ILogger<CalendarDataInitializer> _logger;
        private const string DATA_INITIALIZED_KEY = "calendar_data_initialized";
        
        public CalendarDataInitializer(
            AppDbContext dbContext,
            IHairTransplantEventGenerator eventGenerator,
            ILocalStorageService localStorage,
            ILogger<CalendarDataInitializer> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _eventGenerator = eventGenerator ?? throw new ArgumentNullException(nameof(eventGenerator));
            _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public async Task<bool> NeedsInitializationAsync()
        {
            try
            {
                // Проверяем флаг инициализации в SecureStorage
                bool hasInitFlag = await _localStorage.ContainsKeyAsync(DATA_INITIALIZED_KEY);
                if (hasInitFlag)
                {
                    _logger?.LogInformation("Calendar data already initialized based on flag");
                    return false;
                }
                
                // Если флаг не найден, проверяем наличие событий в базе как запасной вариант
                int eventCount = await _dbContext.Events.CountAsync();
                _logger?.LogInformation("Found {EventCount} events in database", eventCount);
                
                return eventCount == 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking if calendar data needs initialization");
                
                // Если SecureStorage выдал ошибку, проверяем наличие событий как запасной вариант
                try
                {
                    int eventCount = await _dbContext.Events.CountAsync();
                    _logger?.LogInformation("Fallback check: Found {EventCount} events in database", eventCount);
                    return eventCount == 0;
                }
                catch (Exception dbEx)
                {
                    _logger?.LogError(dbEx, "Error checking database for events");
                    // В случае обеих ошибок лучше повторно инициализировать данные
                    return true;
                }
            }
        }
        
        public async Task InitializeDataAsync()
        {
            try
            {
                _logger?.LogInformation("Starting calendar data initialization");
                
                // Генерируем события на первый месяц после пересадки
                DateTime transplantDate = DateTime.Now.Date;
                
                // Создаем события для календаря
                var events = await _eventGenerator.GenerateEventsForPeriodAsync(
                    transplantDate,
                    transplantDate.AddMonths(3));
                
                _logger?.LogInformation("Generated {EventCount} events", events.Count());
                
                // Сохраняем события в базу данных
                await _dbContext.Events.AddRangeAsync(events);
                await _dbContext.SaveChangesAsync();
                
                // Помечаем данные как инициализированные - игнорируем ошибки SecureStorage
                try
                {
                    await _localStorage.SetItemAsync(DATA_INITIALIZED_KEY, new { Initialized = true, Date = DateTime.UtcNow });
                    _logger?.LogInformation("Marked calendar data as initialized in secure storage");
                }
                catch (Exception ex)
                {
                    // Игнорируем ошибку SecureStorage, так как в следующий раз мы все равно проверим наличие событий
                    _logger?.LogWarning(ex, "Could not mark initialization in secure storage, will fallback to database check next time");
                }
                
                _logger?.LogInformation("Calendar data initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing calendar data");
                throw;
            }
        }
    }
} 