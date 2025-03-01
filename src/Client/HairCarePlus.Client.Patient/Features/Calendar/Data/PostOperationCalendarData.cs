using HairCarePlus.Client.Patient.Features.Calendar.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HairCarePlus.Client.Patient.Features.Calendar.Data
{
    public static class PostOperationCalendarData
    {
        public static DateTime OperationDate { get; set; } = DateTime.Now.AddDays(-14);
        
        // Helper method to get calendar data model
        public static CalendarDataModel GetCalendarDataModel()
        {
            return new CalendarDataModel
            {
                OperationDate = OperationDate,
                Events = Events.ToList()
            };
        }
        
        public static readonly List<CalendarEvent> Events = new();
        public static readonly List<Restriction> Restrictions = new();
        public static readonly List<CalendarEvent> Warnings = new();
        public static readonly List<WashingInstructionEvent> WashingInstructions = new();
        public static readonly List<ProgressCheckEvent> ProgressChecks = new();
        public static readonly List<InstructionEvent> Instructions = new();

        static PostOperationCalendarData()
        {
            InitializeEvents();
            InitializeRestrictions();
            InitializeWarnings();
            InitializeWashingInstructions();
            InitializeProgressChecks();
            InitializeInstructions();
        }

        private static void InitializeEvents()
        {
            // Add events here
            Events.Add(new CalendarEvent
            {
                Name = "День операции",
                Description = "День проведения операции по пересадке волос",
                StartDay = 1,
                EndDay = 1,
                Type = EventType.Milestone
            });
            
            // Add more events as needed
        }

        private static void InitializeRestrictions()
        {
            // Add restrictions here
        }

        private static void InitializeWarnings()
        {
            // Add warnings here
        }

        private static void InitializeWashingInstructions()
        {
            // Add washing instructions here
        }

        private static void InitializeProgressChecks()
        {
            // Add progress checks here
        }

        private static void InitializeInstructions()
        {
            // Add instructions here
        }

        public static IEnumerable<int> GetActiveDays()
        {
            var activeDays = new HashSet<int>();
            
            var allEvents = Events
                .Concat<CalendarEvent>(Restrictions)
                .Concat(Warnings)
                .Concat(WashingInstructions)
                .Concat(ProgressChecks)
                .Concat(Instructions);

            foreach (var ev in allEvents)
            {
                activeDays.Add(ev.StartDay);
                if (ev.EndDay.HasValue)
                {
                    activeDays.Add(ev.EndDay.Value);
                }
                
                if (ev.IsRepeating && ev.RepeatIntervalDays.HasValue)
                {
                    var currentDay = ev.StartDay;
                    while (currentDay <= (ev.EndDay ?? 365))
                    {
                        activeDays.Add(currentDay);
                        currentDay += ev.RepeatIntervalDays.Value;
                    }
                }
            }

            return activeDays.OrderBy(d => d);
        }
    }
} 