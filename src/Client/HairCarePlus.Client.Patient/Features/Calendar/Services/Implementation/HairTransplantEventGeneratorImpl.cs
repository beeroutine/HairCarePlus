using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services.Implementation
{
    public class HairTransplantEventGeneratorImpl : IHairTransplantEventGenerator
    {
        public async Task<IEnumerable<CalendarEvent>> GenerateEventsForPeriodAsync(DateTime startDate, DateTime endDate)
        {
            var events = new List<CalendarEvent>();
            
            // Generate events in parallel for better performance
            var tasks = new[]
            {
                GenerateMedicationEventsAsync(startDate),
                GeneratePhotoEventsAsync(startDate),
                GenerateVideoEventsAsync(startDate),
                GenerateRestrictionEventsAsync(startDate),
                GenerateRecommendationEventsAsync(startDate)
            };

            var results = await Task.WhenAll(tasks);
            foreach (var result in results)
            {
                events.AddRange(result);
            }

            return events;
        }

        public async Task<IEnumerable<CalendarEvent>> GenerateMedicationEventsAsync(DateTime startDate)
        {
            var events = new List<CalendarEvent>();
            var now = DateTime.Now;

            await Task.Run(() =>
            {
                // Add medication events for the first week
                for (int i = 0; i < 7; i++)
                {
                    var date = startDate.AddDays(i);
                    events.Add(new CalendarEvent
                    {
                        Title = "Take morning medication",
                        Description = "Take prescribed medication after breakfast",
                        Date = date,
                        StartDate = date,
                        EventType = EventType.MedicationTreatment,
                        Priority = EventPriority.High,
                        TimeOfDay = TimeOfDay.Morning,
                        CreatedAt = now,
                        ModifiedAt = now
                    });
                }
            });

            return events;
        }

        public async Task<IEnumerable<CalendarEvent>> GeneratePhotoEventsAsync(DateTime startDate)
        {
            var events = new List<CalendarEvent>();
            var now = DateTime.Now;

            await Task.Run(() =>
            {
                // Add photo events every 3 days
                for (int i = 0; i < 30; i += 3)
                {
                    var date = startDate.AddDays(i);
                    events.Add(new CalendarEvent
                    {
                        Title = "Take progress photos",
                        Description = "Take photos of transplanted area",
                        Date = date,
                        StartDate = date,
                        EventType = EventType.Photo,
                        Priority = EventPriority.Normal,
                        TimeOfDay = TimeOfDay.Morning,
                        CreatedAt = now,
                        ModifiedAt = now
                    });
                }
            });

            return events;
        }

        public async Task<IEnumerable<CalendarEvent>> GenerateVideoEventsAsync(DateTime startDate)
        {
            var events = new List<CalendarEvent>();
            var now = DateTime.Now;

            await Task.Run(() =>
            {
                // Add video instruction events for the first week
                for (int i = 0; i < 7; i++)
                {
                    var date = startDate.AddDays(i);
                    events.Add(new CalendarEvent
                    {
                        Title = "Watch care instructions",
                        Description = "Watch daily care video instructions",
                        Date = date,
                        StartDate = date,
                        EventType = EventType.Video,
                        Priority = EventPriority.Normal,
                        TimeOfDay = TimeOfDay.Evening,
                        CreatedAt = now,
                        ModifiedAt = now
                    });
                }
            });

            return events;
        }

        public async Task<IEnumerable<CalendarEvent>> GenerateRestrictionEventsAsync(DateTime startDate)
        {
            var events = new List<CalendarEvent>();
            var now = DateTime.Now;

            await Task.Run(() =>
            {
                // Add restriction events
                events.Add(new CalendarEvent
                {
                    Title = "No heavy exercise",
                    Description = "Avoid strenuous physical activity",
                    Date = startDate,
                    StartDate = startDate,
                    EndDate = startDate.AddDays(14),
                    EventType = EventType.CriticalWarning,
                    Priority = EventPriority.Critical,
                    TimeOfDay = TimeOfDay.Morning,
                    CreatedAt = now,
                    ModifiedAt = now,
                    ExpirationDate = startDate.AddDays(14)
                });
            });

            return events;
        }

        public async Task<IEnumerable<CalendarEvent>> GenerateRecommendationEventsAsync(DateTime startDate)
        {
            var events = new List<CalendarEvent>();
            var now = DateTime.Now;

            await Task.Run(() =>
            {
                // Add recommendation events
                events.Add(new CalendarEvent
                {
                    Title = "Sleep position",
                    Description = "Sleep with head elevated",
                    Date = startDate,
                    StartDate = startDate,
                    EndDate = startDate.AddDays(7),
                    EventType = EventType.GeneralRecommendation,
                    Priority = EventPriority.High,
                    TimeOfDay = TimeOfDay.Evening,
                    CreatedAt = now,
                    ModifiedAt = now,
                    ExpirationDate = startDate.AddDays(7)
                });
            });

            return events;
        }
    }
} 