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
        
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly IHairTransplantEventGenerator _eventGenerator;
        private readonly ILocalStorageService _localStorageService;
        private readonly ILogger<CalendarDataInitializer> _logger;
        
        public CalendarDataInitializer(
            IDbContextFactory<AppDbContext> dbFactory,
            IHairTransplantEventGenerator eventGenerator,
            ILocalStorageService localStorageService,
            ILogger<CalendarDataInitializer> logger)
        {
            _dbFactory = dbFactory;
            _eventGenerator = eventGenerator;
            _localStorageService = localStorageService;
            _logger = logger;
        }
        
        public async Task<bool> NeedsInitializationAsync()
        {
            _logger.LogInformation("Checking if calendar data initialization is needed based on Preferences flag and database state.");

            try
            {
                // Check the initialization flag in Preferences
                bool isInitialized = Preferences.Get(DATA_INITIALIZED_KEY, false);

                // If the flag says we are initialized, validate that data actually exists in DB
                if (isInitialized)
                {
                    await using var db = await _dbFactory.CreateDbContextAsync();
                    var eventsCount = await db.Events.CountAsync();
                    if (eventsCount > 0)
                    {
                        _logger.LogInformation("Calendar data is already initialized: Preferences flag set and database contains {EventsCount} events.", eventsCount);
                        return false;
                    }

                    _logger.LogWarning("Preferences flag is set but no events found in database. Re‑initialization required.");
                }
                else
                {
                    _logger.LogInformation("Initialization flag not found in Preferences. Initialization is needed.");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking initialization status. Assuming initialization is needed.");
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
                await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

                // Always clean existing events to avoid duplicates or stale data
                var existingEvents = await db.Events.CountAsync(cancellationToken);
                if (existingEvents > 0)
                {
                    _logger.LogInformation("Removing {ExistingEvents} existing events before re‑initialization.", existingEvents);
                    db.Events.RemoveRange(db.Events);
                    await db.SaveChangesAsync(cancellationToken);
                }

                // Generate and store calendar events
                _logger.LogInformation("Generating calendar events for initialization.");
                var startDate = DateTime.Today;
                var endDate = startDate.AddDays(364); // Generate events for 1 year
                var events = (await _eventGenerator.GenerateEventsForPeriodAsync(startDate, endDate)).ToList();

                // Generate and include restriction events from dedicated schedule
                var restrictionEvents = (await _eventGenerator.GenerateRestrictionEventsAsync(startDate)).ToList();

                _logger.LogInformation("Generated {EventCount} standard events and {RestrictionCount} restriction events for initialization.",
                                        events.Count, restrictionEvents.Count);

                events.AddRange(restrictionEvents);

                await db.Events.AddRangeAsync(events, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);

                var total = events.Count;
                _logger.LogInformation("Successfully saved {EventCount} events to the database (including restrictions).", total);

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