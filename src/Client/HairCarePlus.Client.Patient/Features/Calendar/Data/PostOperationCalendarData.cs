using HairCarePlus.Client.Patient.Features.Calendar.Models;
using System.Collections.Generic;
using System.Linq;

namespace HairCarePlus.Client.Patient.Features.Calendar.Data
{
    public static class PostOperationCalendarData
    {
        public static readonly List<CalendarEvent> Events = new()
        {
            // Day 0 - Operation Day
            new CalendarEvent
            {
                Name = "День операции",
                Description = "День проведения операции по пересадке волос",
                StartDay = 0,
                Type = EventType.Milestone
            },

            // Medications
            new MedicationEvent
            {
                Name = "Прием Prednol/Precort",
                Description = "3 таблетки единовременно после завтрака",
                StartDay = 1,
                EndDay = 1,
                Type = EventType.Medication,
                MedicationName = "Prednol/Precort",
                Dosage = "3 таблетки",
                TimesPerDay = 1,
                Instructions = "Принимать после завтрака"
            },
            new MedicationEvent
            {
                Name = "Прием Prednol/Precort",
                Description = "3 таблетки единовременно после завтрака",
                StartDay = 2,
                EndDay = 2,
                Type = EventType.Medication,
                MedicationName = "Prednol/Precort",
                Dosage = "3 таблетки",
                TimesPerDay = 1,
                Instructions = "Принимать после завтрака"
            },
            new MedicationEvent
            {
                Name = "Прием Prednol/Precort",
                Description = "2 таблетки единовременно после завтрака",
                StartDay = 3,
                EndDay = 4,
                Type = EventType.Medication,
                MedicationName = "Prednol/Precort",
                Dosage = "2 таблетки",
                TimesPerDay = 1,
                Instructions = "Принимать после завтрака"
            },
            new MedicationEvent
            {
                Name = "Прием Ciprasid/Cipro/Cipronatin",
                Description = "По одной таблетке утром и вечером после еды",
                StartDay = 1,
                EndDay = 7,
                Type = EventType.Medication,
                MedicationName = "Ciprasid/Cipro/Cipronatin",
                Dosage = "1 таблетка",
                TimesPerDay = 2,
                Instructions = "Принимать после еды утром и вечером"
            },
            new MedicationEvent
            {
                Name = "Apronax/Aprol for/Parol Arveles",
                Description = "Обезболивающее при необходимости",
                StartDay = 1,
                EndDay = 7,
                Type = EventType.Medication,
                MedicationName = "Apronax/Aprol for/Parol Arveles",
                Dosage = "1 таблетка",
                TimesPerDay = 2,
                Instructions = "Принимать при болевых ощущениях, не более 2 таблеток в сутки, перерыв между приемами 4 часа",
                IsOptional = true
            },

            // Photo Upload Events
            new PhotoUploadEvent
            {
                Name = "Ежедневное фото",
                Description = "Загрузка фотографий зоны пересадки и донорской зоны",
                StartDay = 1,
                EndDay = 10,
                Type = EventType.PhotoUpload,
                RequiredAreas = new[] { "Зона пересадки", "Донорская зона" },
                RequiredAngles = new[] { "Фронтальный", "Боковой", "Задний" }
            },
            new PhotoUploadEvent
            {
                Name = "Ежемесячное фото",
                Description = "Ежемесячный фотоотчет прогресса",
                StartDay = 30,
                EndDay = 365,
                Type = EventType.PhotoUpload,
                IsRepeating = true,
                RepeatIntervalDays = 30,
                RequiredAreas = new[] { "Зона пересадки", "Донорская зона" }
            },

            // Milestones and Instructions
            new MilestoneEvent
            {
                Name = "Смывание корочек",
                Description = "Инструкция по правильному смыванию корочек",
                StartDay = 10,
                Type = EventType.Milestone,
                Achievement = "Готовность к смыванию корочек",
                UnlockedActivities = new[] { 
                    "Ношение головных уборов",
                    "Сон в любом положении",
                    "Умеренное употребление алкоголя"
                }
            },
            new CalendarEvent
            {
                Name = "Начало PRP-терапии",
                Description = "Рекомендуется начать курс плазмотерапии",
                StartDay = 30,
                Type = EventType.PRP,
                IsRepeating = true,
                RepeatIntervalDays = 30
            },
            new MilestoneEvent
            {
                Name = "Месяц после операции",
                Description = "Снятие основных ограничений",
                StartDay = 30,
                Type = EventType.Milestone,
                UnlockedActivities = new[] {
                    "Умеренные физические нагрузки",
                    "Посещение бассейна",
                    "Посещение сауны/бани"
                }
            },
            new MilestoneEvent
            {
                Name = "Итоговый результат",
                Description = "Создание коллажа до/после",
                StartDay = 365,
                Type = EventType.Milestone,
                Achievement = "Годовой результат пересадки"
            }
        };

        public static readonly List<Restriction> Restrictions = new()
        {
            new Restriction
            {
                Name = "Запрет на наклоны головы",
                Description = "Не наклонять голову вниз",
                StartDay = 0,
                EndDay = 3,
                Type = EventType.Restriction,
                Reason = "Предотвращение отека в области лба",
                IsCritical = true
            },
            new Restriction
            {
                Name = "Запрет на головные уборы",
                Description = "Нельзя носить шапки и другие головные уборы",
                StartDay = 0,
                EndDay = 10,
                Type = EventType.Restriction,
                Reason = "Защита зоны пересадки"
            },
            new Restriction
            {
                Name = "Запрет на курение",
                Description = "Полный запрет на курение",
                StartDay = 0,
                EndDay = 2,
                Type = EventType.Restriction,
                Reason = "Влияет на приживаемость",
                IsCritical = true
            },
            new Restriction
            {
                Name = "Запрет на алкоголь",
                Description = "Полный запрет на алкоголь",
                StartDay = 0,
                EndDay = 10,
                Type = EventType.Restriction,
                Reason = "Влияет на приживаемость и может взаимодействовать с лекарствами",
                IsCritical = true
            },
            new Restriction
            {
                Name = "Запрет на активный спорт",
                Description = "Запрет на активные физические нагрузки",
                StartDay = 0,
                EndDay = 30,
                Type = EventType.Restriction,
                Reason = "Защита зоны пересадки от травм и повышенного потоотделения",
                RecommendedAlternative = "Легкие прогулки после 15 дня"
            },
            new Restriction
            {
                Name = "Запрет на интимную близость",
                Description = "Воздержание от половой активности",
                StartDay = 0,
                EndDay = 7,
                Type = EventType.Restriction,
                Reason = "Защита от повышения давления и потоотделения"
            },
            new Restriction
            {
                Name = "Запрет на окрашивание волос",
                Description = "Нельзя красить волосы",
                StartDay = 0,
                EndDay = 240, // 8 months
                Type = EventType.Restriction,
                Reason = "Защита волосяных фолликулов от химического воздействия"
            },
            new Restriction
            {
                Name = "Запрет на водные процедуры",
                Description = "Запрет на посещение бассейна, сауны, бани",
                StartDay = 0,
                EndDay = 30,
                Type = EventType.Restriction,
                Reason = "Защита от инфекций и воздействия высоких температур"
            }
        };

        public static readonly List<CalendarEvent> Warnings = new()
        {
            new CalendarEvent
            {
                Name = "Возможное начало болевых ощущений",
                Description = "Может появиться тянущая, ноющая боль в зоне пересадки - это нормально",
                StartDay = 3,
                EndDay = 4,
                Type = EventType.Warning
            },
            new CalendarEvent
            {
                Name = "Шоковое выпадение",
                Description = "Может начаться выпадение пересаженных волос - это нормальный процесс",
                StartDay = 21,
                EndDay = 28,
                Type = EventType.Warning
            },
            new CalendarEvent
            {
                Name = "Возможные прыщики",
                Description = "Могут появляться прыщики на донорской зоне и в зоне пересадки - это нормальный процесс восстановления",
                StartDay = 60,
                EndDay = 180,
                Type = EventType.Warning
            }
        };

        public static readonly List<WashingInstructionEvent> WashingInstructions = new()
        {
            new WashingInstructionEvent
            {
                Name = "Первое мытье головы",
                Description = "Инструкция по первому мытью головы после операции",
                StartDay = 1,
                EndDay = 10,
                Type = EventType.WashingInstruction,
                Steps = new List<WashingStep>
                {
                    new WashingStep
                    {
                        Order = 1,
                        Description = "Нанесите спрей на всю область, включая донорскую зону",
                        DurationInMinutes = 5,
                        Tips = new[] { "Убедитесь, что спрей равномерно распределен" }
                    },
                    new WashingStep
                    {
                        Order = 2,
                        Description = "Подождите 5-10 минут после нанесения спрея",
                        DurationInMinutes = 10,
                        Tips = new[] { "Используйте таймер для точного отсчета времени" }
                    },
                    new WashingStep
                    {
                        Order = 3,
                        Description = "Смойте прохладной водой без напора",
                        DurationInMinutes = 5,
                        Tips = new[] { 
                            "Вода должна быть комнатной температуры",
                            "Избегайте прямого напора воды на зону пересадки"
                        }
                    },
                    new WashingStep
                    {
                        Order = 4,
                        Description = "Высушите области бумажным полотенцем",
                        DurationInMinutes = 5,
                        Tips = new[] { 
                            "Промокающими движениями, не растирая",
                            "Используйте только мягкие бумажные полотенца"
                        }
                    }
                },
                RequiredItems = new[] {
                    "Специальный спрей",
                    "Бумажные полотенца",
                    "Таймер"
                },
                Warnings = new[] {
                    "Не тереть зону пересадки",
                    "Избегать горячей воды",
                    "Не использовать обычное полотенце"
                }
            }
        };

        public static readonly List<ProgressCheckEvent> ProgressChecks = new()
        {
            new ProgressCheckEvent
            {
                Name = "Проверка состояния - первая неделя",
                Description = "Оценка состояния и прогресса восстановления",
                StartDay = 7,
                Type = EventType.ProgressCheck,
                Phase = RecoveryPhase.EarlyRecovery,
                ExpectedChanges = new[] {
                    "Уменьшение отечности",
                    "Формирование корочек",
                    "Небольшой дискомфорт в донорской зоне"
                },
                CheckPoints = new[] {
                    "Отсутствие повышенной температуры",
                    "Нормальное заживление ран",
                    "Отсутствие сильной боли"
                },
                NormalConditions = new Dictionary<string, string>
                {
                    {"Корочки", "Равномерные, не кровоточат"},
                    {"Отек", "Уменьшается с каждым днем"},
                    {"Боль", "Незначительная, терпимая"}
                },
                WarningSignals = new Dictionary<string, string>
                {
                    {"Температура", "Выше 38°C"},
                    {"Кровотечение", "Активное кровотечение из ран"},
                    {"Боль", "Сильная, нарастающая боль"}
                }
            },
            new ProgressCheckEvent
            {
                Name = "Проверка состояния - первый месяц",
                Description = "Оценка состояния через месяц после операции",
                StartDay = 30,
                Type = EventType.ProgressCheck,
                Phase = RecoveryPhase.Healing,
                ExpectedChanges = new[] {
                    "Начало выпадения пересаженных волос",
                    "Полное заживление донорской зоны",
                    "Отсутствие корочек"
                },
                CheckPoints = new[] {
                    "Состояние кожи головы",
                    "Процесс выпадения волос",
                    "Заживление донорской зоны"
                },
                NormalConditions = new Dictionary<string, string>
                {
                    {"Выпадение волос", "Равномерное, безболезненное"},
                    {"Кожа головы", "Без воспаления и раздражения"},
                    {"Донорская зона", "Заживление завершено"}
                },
                WarningSignals = new Dictionary<string, string>
                {
                    {"Воспаление", "Покраснение и отек"},
                    {"Зуд", "Сильный постоянный зуд"},
                    {"Боль", "Острая боль в местах пересадки"}
                }
            }
        };

        public static readonly List<InstructionEvent> Instructions = new()
        {
            new InstructionEvent
            {
                Name = "Инструкция по смыванию корочек",
                Description = "Подробная инструкция по правильному смыванию корочек",
                StartDay = 10,
                Type = EventType.Instruction,
                Steps = new[] {
                    "Намочите голову теплой водой",
                    "Нанесите специальный шампунь",
                    "Мягкими круговыми движениями массируйте кожу",
                    "Тщательно промойте волосы теплой водой",
                    "Просушите волосы промокающими движениями"
                },
                Tips = new[] {
                    "Не используйте горячую воду",
                    "Избегайте сильного трения",
                    "При необходимости повторите процедуру"
                },
                Cautions = new[] {
                    "Не пытайтесь удалить корочки силой",
                    "Прекратите процедуру при появлении боли",
                    "Не используйте обычные шампуни"
                }
            }
        };

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