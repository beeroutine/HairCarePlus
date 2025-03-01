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
            Restrictions.Add(new Restriction
            {
                Name = "Ограничение физических нагрузок",
                Description = "Избегайте интенсивных физических нагрузок",
                StartDay = 1,
                EndDay = 14,
                Type = EventType.Restriction,
                Reason = "Риск повышения кровяного давления",
                IsCritical = true,
                RecommendedAlternative = "Легкая ходьба"
            });

            Restrictions.Add(new Restriction
            {
                Name = "Ограничение контакта с водой",
                Description = "Избегайте прямого контакта с водой в области пересадки",
                StartDay = 1,
                EndDay = 7,
                Type = EventType.Restriction,
                Reason = "Риск инфекции",
                IsCritical = true,
                RecommendedAlternative = "Использование специального шампуня по инструкции"
            });
        }

        private static void InitializeWarnings()
        {
            Warnings.Add(new CalendarEvent
            {
                Name = "Возможное покраснение",
                Description = "Возможно появление покраснения в области пересадки",
                StartDay = 1,
                EndDay = 5,
                Type = EventType.Warning
            });

            Warnings.Add(new CalendarEvent
            {
                Name = "Отёк",
                Description = "Возможно появление отёка в области лба",
                StartDay = 2,
                EndDay = 4,
                Type = EventType.Warning
            });
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
            Instructions.Add(new InstructionEvent
            {
                Name = "Уход за областью пересадки",
                Description = "Пошаговая инструкция по уходу за областью пересадки",
                StartDay = 1,
                EndDay = 7,
                Type = EventType.Instruction,
                Steps = new[]
                {
                    "Осторожно промывайте область специальным раствором",
                    "Не трите и не чешите область пересадки",
                    "Спите с приподнятой головой"
                },
                Tips = new[]
                {
                    "Используйте мягкую подушку",
                    "Держите область пересадки в чистоте"
                },
                Cautions = new[]
                {
                    "Избегайте прямых солнечных лучей",
                    "Не используйте обычные шампуни"
                }
            });
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