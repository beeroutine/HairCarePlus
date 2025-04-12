using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using HairCarePlus.Client.Patient.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services.Implementation
{
    public class CalendarDataInitializer : IDataInitializer
    {
        private const string DATA_INITIALIZED_KEY = "CalendarDataInitialized";
        
        private readonly AppDbContext _dbContext;
        private readonly IHairTransplantEventGenerator _eventGenerator;
        private readonly ILocalStorageService _localStorageService;
        private readonly ILogger<CalendarDataInitializer> _logger;
        
        public CalendarDataInitializer(
            IDbContextFactory<AppDbContext> dbContextFactory,
            IHairTransplantEventGenerator eventGenerator,
            ILocalStorageService localStorageService,
            ILogger<CalendarDataInitializer> logger)
        {
            _dbContext = dbContextFactory.CreateDbContext();
            _eventGenerator = eventGenerator;
            _localStorageService = localStorageService;
            _logger = logger;
        }
        
        public async Task<bool> NeedsInitializationAsync()
        {
            _logger.LogInformation("Checking if calendar data initialization is needed based on Preferences flag.");
            
            try
            {
                // Check the initialization flag in Preferences
                bool isInitialized = Preferences.Get(DATA_INITIALIZED_KEY, false);
                if (isInitialized)
                {
                    _logger.LogInformation("Calendar data is already initialized according to Preferences flag.");
                    return false;
                }
                else
                {
                    _logger.LogInformation("Initialization flag not found in Preferences. Initialization is needed.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking initialization flag in Preferences. Assuming initialization is needed.");
                return true;
            }
        }
        
        public async Task InitializeDataAsync()
        {
            await InitializeDataAsync(CancellationToken.None);
        }
        
        public async Task InitializeDataAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting calendar data initialization process.");
            
            try
            {
                // Double-check the flag in Preferences to avoid race conditions
                bool isInitialized = Preferences.Get(DATA_INITIALIZED_KEY, false);
                if (isInitialized)
                {
                    _logger.LogInformation("Calendar data initialization skipped as it is already initialized.");
                    return;
                }

                // Generate and store events
                _logger.LogInformation("Generating calendar events for initialization.");
                var startDate = DateTime.Today;
                var endDate = startDate.AddMonths(6); // Generate events for next 6 months
                var events = await _eventGenerator.GenerateEventsForPeriodAsync(startDate, endDate);
                _logger.LogInformation("Generated {EventCount} events for initialization.", events.Count());

                await _dbContext.Events.AddRangeAsync(events, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully saved {EventCount} events to the database.", events.Count());

                // Set the initialization flag in Preferences
                Preferences.Set(DATA_INITIALIZED_KEY, true);
                _logger.LogInformation("Calendar data initialization completed and flag set in Preferences.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize calendar data.");
                throw;
            }
        }
    }
} 