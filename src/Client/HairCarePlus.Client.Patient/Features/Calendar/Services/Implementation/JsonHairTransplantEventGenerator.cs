using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using Microsoft.Maui.Storage;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services.Implementation
{
    /// <summary>
    /// Data‑driven generator: преобразует расписание из JSON‑файла Resources/Raw/HairTransplantSchedule.json 
    /// в список CalendarEvent. Позволяет обновлять расписание без перекомпиляции кода.
    /// </summary>
    public sealed class JsonHairTransplantEventGenerator : IHairTransplantEventGenerator
    {
        private const string ScheduleFileName = "HairTransplantSchedule.json";
        private const string RestrictionScheduleFileName = "RestrictionSchedule.json";

        private readonly Lazy<Task<List<ScheduleDay>>> _lazySchedule;
        private readonly Lazy<Task<List<RestrictionItem>>> _lazyRestrictionSchedule;

        public JsonHairTransplantEventGenerator()
        {
            _lazySchedule = new Lazy<Task<List<ScheduleDay>>>(LoadScheduleAsync);
            _lazyRestrictionSchedule = new Lazy<Task<List<RestrictionItem>>>(LoadRestrictionScheduleAsync);
        }

        #region Public IHairTransplantEventGenerator implementation

        public async Task<IEnumerable<CalendarEvent>> GenerateEventsForPeriodAsync(DateTime startDate, DateTime endDate)
        {
            var schedule = await _lazySchedule.Value;
            var events = new List<CalendarEvent>();
            foreach (var day in schedule)
            {
                var date = startDate.Date.AddDays(day.Day - 1);
                if (date.Date < startDate.Date || date.Date > endDate.Date) continue;
                events.AddRange(ToCalendarEvents(day, date));
            }
            return events;
        }

        public async Task<IEnumerable<CalendarEvent>> GenerateMedicationEventsAsync(DateTime startDate)
        {
            var schedule = await _lazySchedule.Value;
            var list = new List<CalendarEvent>();
            foreach (var day in schedule)
            {
                var date = startDate.Date.AddDays(day.Day - 1);
                if (date < startDate) continue;
                foreach (var task in day.Tasks.Where(t => t.Type == "MedicationTreatment"))
                {
                    list.Add(ToCalendarEvent(task, date));
                }
            }
            return list;
        }

        public async Task<IEnumerable<CalendarEvent>> GeneratePhotoEventsAsync(DateTime startDate)
        {
            var schedule = await _lazySchedule.Value;
            return FilterByType(schedule, startDate, "Photo");
        }

        public async Task<IEnumerable<CalendarEvent>> GenerateVideoEventsAsync(DateTime startDate)
        {
            var schedule = await _lazySchedule.Value;
            return FilterByType(schedule, startDate, "Video");
        }

        public async Task<IEnumerable<CalendarEvent>> GenerateRestrictionEventsAsync(DateTime startDate)
        {
            var restrictionSchedule = await _lazyRestrictionSchedule.Value;

            var events = new List<CalendarEvent>();

            foreach (var item in restrictionSchedule)
            {
                var restrictionStart = startDate.Date.AddDays(item.StartDay - 1);
                var restrictionEnd = startDate.Date.AddDays(item.EndDay - 1);

                if (restrictionEnd < startDate.Date) continue;

                var now = DateTime.UtcNow;

                events.Add(new CalendarEvent
                {
                    Title = item.Title,
                    Description = item.Description ?? string.Empty,
                    Date = restrictionStart,
                    StartDate = restrictionStart,
                    EndDate = restrictionEnd,
                    EventType = EventType.CriticalWarning,
                    Priority = EventPriority.Normal,
                    TimeOfDay = TimeOfDay.Morning,
                    CreatedAt = now,
                    ModifiedAt = now
                });
            }

            return events;
        }

        public async Task<IEnumerable<CalendarEvent>> GenerateRecommendationEventsAsync(DateTime startDate)
        {
            var schedule = await _lazySchedule.Value;
            return FilterByType(schedule, startDate, "GeneralRecommendation");
        }

        #endregion

        #region Helpers

        private static IEnumerable<CalendarEvent> FilterByType(IEnumerable<ScheduleDay> schedule, DateTime startDate, string type)
        {
            var list = new List<CalendarEvent>();
            foreach (var day in schedule)
            {
                var date = startDate.Date.AddDays(day.Day - 1);
                if (date < startDate) continue;
                foreach (var task in day.Tasks.Where(t => t.Type == type))
                {
                    list.Add(ToCalendarEvent(task, date));
                }
            }
            return list;
        }

        private static IEnumerable<CalendarEvent> ToCalendarEvents(ScheduleDay day, DateTime date)
            => day.Tasks.Select(task => ToCalendarEvent(task, date));

        private static CalendarEvent ToCalendarEvent(ScheduleTask task, DateTime date)
        {
            var now = DateTime.UtcNow;
            return new CalendarEvent
            {
                Title = task.Title,
                Description = string.Empty,
                Date = date,
                StartDate = date,
                EventType = MapEventType(task.Type),
                Priority = EventPriority.Normal,
                TimeOfDay = TimeOfDay.Morning,
                CreatedAt = now,
                ModifiedAt = now
            };
        }

        private static EventType MapEventType(string type) => type switch
        {
            "MedicationTreatment" => EventType.MedicationTreatment,
            "MedicalVisit" => EventType.MedicalVisit,
            "Photo" => EventType.Photo,
            "Video" => EventType.Video,
            "GeneralRecommendation" => EventType.GeneralRecommendation,
            "CriticalWarning" => EventType.CriticalWarning,
            _ => EventType.GeneralRecommendation
        };

        private static async Task<List<ScheduleDay>> LoadScheduleAsync()
        {
            await using var stream = await FileSystem.OpenAppPackageFileAsync(ScheduleFileName);
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            var schedule = JsonSerializer.Deserialize<List<ScheduleDay>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return schedule ?? new List<ScheduleDay>();
        }

        private static async Task<List<RestrictionItem>> LoadRestrictionScheduleAsync()
        {
            await using var stream = await FileSystem.OpenAppPackageFileAsync(RestrictionScheduleFileName);
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            var list = JsonSerializer.Deserialize<List<RestrictionItem>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return list ?? new List<RestrictionItem>();
        }

        #endregion

        #region DTOs
        private sealed class ScheduleDay
        {
            public int Day { get; set; }
            public List<ScheduleTask> Tasks { get; set; } = new();
        }

        private sealed class ScheduleTask
        {
            public string Title { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
        }

        private sealed class RestrictionItem
        {
            public string Title { get; set; } = string.Empty;
            public int StartDay { get; set; }
            public int EndDay { get; set; }
            public int DurationDays { get; set; }
            public string? Description { get; set; }
        }
        #endregion
    }
} 