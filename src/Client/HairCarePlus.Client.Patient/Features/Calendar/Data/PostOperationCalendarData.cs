using HairCarePlus.Client.Patient.Features.Calendar.Models;
using System.Collections.Generic;
using System.Linq;

namespace HairCarePlus.Client.Patient.Features.Calendar.Data
{
    public static class PostOperationCalendarData
    {
        public static readonly List<CalendarEvent> Events = new()
        {
            // Day 1 - Operation Day
            new CalendarEvent
            {
                Name = "День операции",
                Description = "День проведения операции по пересадке волос",
                StartDay = 1,
                EndDay = 1,
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
                Name = "Защита области трансплантации",
                Description = "Соблюдать осторожность, чтобы не ударить и не поцарапать область трансплантации",
                StartDay = 1,
                EndDay = 180, // 6 месяцев
                Type = EventType.Restriction,
                Reason = "Защита пересаженных волосяных фолликулов",
                IsCritical = true
            },
            new Restriction
            {
                Name = "Запрет на наклоны головы",
                Description = "Не наклонять голову вперед и не совершать длительных прогулок",
                StartDay = 1,
                EndDay = 3,
                Type = EventType.Restriction,
                Reason = "Предотвращение отека в области лба",
                IsCritical = true
            },
            new Restriction
            {
                Name = "Запрет на стрижку машинкой",
                Description = "Стричь волосы на зоне пересадки только ножницами",
                StartDay = 1,
                EndDay = 120, // 4 месяца
                Type = EventType.Restriction,
                Reason = "Защита пересаженных волос",
                RecommendedAlternative = "Использовать только ножницы для стрижки"
            },
            new Restriction
            {
                Name = "Запрет на окрашивание волос",
                Description = "Запрещено использовать краску для волос и гель",
                StartDay = 1,
                EndDay = 180, // 6 месяцев
                Type = EventType.Restriction,
                Reason = "Защита волосяных фолликулов от химического воздействия"
            },
            new Restriction
            {
                Name = "Запрет на активный спорт",
                Description = "Запрет на занятия спортом с риском травмы (бокс, футбол, баскетбол, бодибилдинг)",
                StartDay = 1,
                EndDay = 30,
                Type = EventType.Restriction,
                Reason = "Защита от травм и повышенного потоотделения",
                RecommendedAlternative = "Легкие прогулки после 15 дня"
            },
            new Restriction
            {
                Name = "Защита от солнца",
                Description = "Необходимо защищать зону пересадки от прямых солнечных лучей",
                StartDay = 1,
                EndDay = 60, // 2 месяца
                Type = EventType.Restriction,
                Reason = "Защита от солнечного воздействия",
                RecommendedAlternative = "Использовать головной убор из дышащей ткани и натуральный солнцезащитный крем"
            },
            new Restriction
            {
                Name = "Запрет на водные процедуры",
                Description = "Запрет на посещение бассейна, сауны, турецкой бани, солярия",
                StartDay = 1,
                EndDay = 60, // 2 месяца
                Type = EventType.Restriction,
                Reason = "Защита от инфекций и воздействия высоких температур"
            },
            new Restriction
            {
                Name = "Запрет на купание в море",
                Description = "Запрещено купание в море",
                StartDay = 1,
                EndDay = 30,
                Type = EventType.Restriction,
                Reason = "Защита от инфекций и соли"
            },
            new Restriction
            {
                Name = "Запрет на интимную близость",
                Description = "Воздержание от половой активности",
                StartDay = 1,
                EndDay = 7,
                Type = EventType.Restriction,
                Reason = "Защита от повышения давления и потоотделения"
            },
            new Restriction
            {
                Name = "Запрет на алкоголь",
                Description = "Полный запрет на употребление алкоголя во время приема антибиотиков",
                StartDay = 1,
                EndDay = 7,
                Type = EventType.Restriction,
                Reason = "Взаимодействие с лекарствами",
                IsCritical = true
            }
        };

        public static readonly List<CalendarEvent> Warnings = new()
        {
            new CalendarEvent
            {
                Name = "Кровотечение в первую ночь",
                Description = "В области затылка может вытекать кровь с физраствором - это нормально. Рекомендуется снять одежду в номере во избежание загрязнения.",
                StartDay = 1,
                EndDay = 2,
                Type = EventType.Warning
            },
            new CalendarEvent
            {
                Name = "Возможный отек",
                Description = "Может появиться отек в области лба, который уменьшится в течение 3-5 дней. Отек вызывает только внешний дискомфорт и не опасен для здоровья.",
                StartDay = 1,
                EndDay = 5,
                Type = EventType.Warning
            },
            new CalendarEvent
            {
                Name = "Чувствительность в донорской зоне",
                Description = "Чувствительность в области затылка может сохраняться до 4 недель. При зуде можно использовать антигистаминные препараты по согласованию с врачом.",
                StartDay = 1,
                EndDay = 28,
                Type = EventType.Warning
            },
            new CalendarEvent
            {
                Name = "Шоковое выпадение",
                Description = "Начало выпадения пересаженных волос - это нормальный процесс. Волосяные фолликулы остаются внутри кожи, выпадают только волосы.",
                StartDay = 21,
                EndDay = 28,
                Type = EventType.Warning
            },
            new CalendarEvent
            {
                Name = "Возможные прыщики",
                Description = "Могут появляться прыщики на донорской зоне и в зоне пересадки - это нормальный процесс восстановления. Соблюдайте гигиену, не выдавливайте прыщики.",
                StartDay = 60,
                EndDay = 180,
                Type = EventType.Warning
            },
            new CalendarEvent
            {
                Name = "Начало роста новых волос",
                Description = "Через три-четыре месяца на зоне пересадки начнут расти новые волосы.",
                StartDay = 90,
                EndDay = 120,
                Type = EventType.Warning
            }
        };

        public static readonly List<WashingInstructionEvent> WashingInstructions = new()
        {
            new WashingInstructionEvent
            {
                Name = "Использование морской воды",
                Description = "Инструкция по использованию морской воды против зуда",
                StartDay = 3,
                EndDay = 10,
                Type = EventType.WashingInstruction,
                Steps = new List<WashingStep>
                {
                    new WashingStep
                    {
                        Order = 1,
                        Description = "Приобретите морскую воду в аптеке",
                        DurationInMinutes = 0,
                        Tips = new[] { "Используйте только аптечную морскую воду" }
                    },
                    new WashingStep
                    {
                        Order = 2,
                        Description = "Нанесите морскую воду на зудящие участки",
                        DurationInMinutes = 1,
                        Tips = new[] { "Наносите аккуратно, не растирая" }
                    }
                },
                RequiredItems = new[] {
                    "Морская вода из аптеки"
                },
                Warnings = new[] {
                    "Не использовать обычную морскую воду",
                    "Не тереть зону пересадки"
                }
            },
            new WashingInstructionEvent
            {
                Name = "Первое мытье головы и смывание корочек",
                Description = "Инструкция по первому мытью головы и смыванию корочек после операции",
                StartDay = 10,
                EndDay = 15,
                Type = EventType.WashingInstruction,
                Steps = new List<WashingStep>
                {
                    new WashingStep
                    {
                        Order = 1,
                        Description = "Намочите голову теплой водой",
                        DurationInMinutes = 2,
                        Tips = new[] { "Вода должна быть комнатной температуры" }
                    },
                    new WashingStep
                    {
                        Order = 2,
                        Description = "Нанесите специальный шампунь",
                        DurationInMinutes = 1,
                        Tips = new[] { "Используйте только рекомендованный шампунь" }
                    },
                    new WashingStep
                    {
                        Order = 3,
                        Description = "Мягкими круговыми движениями массируйте кожу",
                        DurationInMinutes = 5,
                        Tips = new[] { 
                            "Не используйте ногти",
                            "Движения должны быть очень легкими"
                        }
                    },
                    new WashingStep
                    {
                        Order = 4,
                        Description = "Тщательно промойте волосы теплой водой",
                        DurationInMinutes = 3,
                        Tips = new[] { 
                            "Убедитесь, что весь шампунь смыт",
                            "Вода должна быть комнатной температуры"
                        }
                    }
                },
                RequiredItems = new[] {
                    "Специальный шампунь от клиники",
                    "Мягкое полотенце"
                },
                Warnings = new[] {
                    "Не использовать обычные шампуни",
                    "Не использовать горячую воду",
                    "Не тереть кожу ногтями",
                    "Не пытаться удалить корочки силой"
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