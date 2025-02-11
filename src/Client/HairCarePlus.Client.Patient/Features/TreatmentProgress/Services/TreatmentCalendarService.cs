using System;
using System.Collections.Generic;
using HairCarePlus.Client.Patient.Features.TreatmentProgress.Models;

namespace HairCarePlus.Client.Patient.Features.TreatmentProgress.Services
{
    public class TreatmentCalendarService
    {
        public List<TreatmentEvent> GenerateCalendar(DateTime surgeryDate)
        {
            var events = new List<TreatmentEvent>();
            
            // Добавляем прием медикаментов
            AddMedications(events, surgeryDate);
            
            // Добавляем ограничения
            AddRestrictions(events, surgeryDate);
            
            // Добавляем фотоотчеты
            AddPhotoReports(events, surgeryDate);
            
            // Добавляем уход
            AddCareEvents(events, surgeryDate);
            
            // Добавляем этапы восстановления
            AddMilestones(events, surgeryDate);
            
            // Добавляем плазмотерапию
            AddPlasmaTherapy(events, surgeryDate);

            return events;
        }

        private void AddMedications(List<TreatmentEvent> events, DateTime surgeryDate)
        {
            // Преднол/Прекорт
            var prednolSchedule = new[]
            {
                (day: 1, count: 3, description: "3 таблетки после завтрака"),
                (day: 2, count: 3, description: "3 таблетки после завтрака"),
                (day: 3, count: 2, description: "2 таблетки после завтрака"),
                (day: 4, count: 2, description: "2 таблетки после завтрака")
            };

            foreach (var schedule in prednolSchedule)
            {
                events.Add(new MedicationEvent
                {
                    Date = surgeryDate.AddDays(schedule.day),
                    Type = EventType.Medication,
                    MedicationType = MedicationType.Prednol,
                    Title = $"Прием Преднол/Прекорт",
                    Description = schedule.description,
                    DosageCount = schedule.count,
                    DosageUnit = "таблетки",
                    IsMorning = true,
                    IsEvening = false,
                    WithFood = "после завтрака",
                    IsRequired = true
                });
            }

            // Ципрасид/Ципро/Ципронатин
            for (int day = 1; day <= 7; day++)
            {
                var date = surgeryDate.AddDays(day);
                events.Add(new MedicationEvent
                {
                    Date = date,
                    Type = EventType.Medication,
                    MedicationType = MedicationType.Ciprasid,
                    Title = "Прием Ципрасид утром",
                    Description = "1 таблетка после завтрака",
                    DosageCount = 1,
                    DosageUnit = "таблетка",
                    IsMorning = true,
                    IsEvening = false,
                    WithFood = "после еды",
                    IsRequired = true
                });

                events.Add(new MedicationEvent
                {
                    Date = date,
                    Type = EventType.Medication,
                    MedicationType = MedicationType.Ciprasid,
                    Title = "Прием Ципрасид вечером",
                    Description = "1 таблетка после ужина",
                    DosageCount = 1,
                    DosageUnit = "таблетка",
                    IsMorning = false,
                    IsEvening = true,
                    WithFood = "после еды",
                    IsRequired = true
                });
            }

            // Апронакс (обезболивающее)
            for (int day = 1; day <= 7; day++)
            {
                events.Add(new MedicationEvent
                {
                    Date = surgeryDate.AddDays(day),
                    Type = EventType.Medication,
                    MedicationType = MedicationType.Apronax,
                    Title = "Апронакс (при необходимости)",
                    Description = "При болях. Максимум 2 таблетки в день, перерыв 4 часа",
                    DosageCount = 1,
                    DosageUnit = "таблетка",
                    IsRequired = false
                });
            }

            // Витамины на весь год (начиная с 30-го дня)
            for (int day = 30; day <= 365; day++)
            {
                events.Add(new MedicationEvent
                {
                    Date = surgeryDate.AddDays(day),
                    Type = EventType.Vitamins,
                    MedicationType = MedicationType.Vitamins,
                    Title = "Прием витаминов",
                    Description = "Ежедневный прием комплекса витаминов",
                    IsRequired = true
                });
            }
        }

        private void AddRestrictions(List<TreatmentEvent> events, DateTime surgeryDate)
        {
            var restrictions = new[]
            {
                (type: RestrictionType.BendingDown, days: 4, reason: "Предотвращение отеков", description: "Не наклоняться вниз, держать телефон и книги на уровне глаз"),
                (type: RestrictionType.SleepingPosition, days: 10, reason: "Правильное заживление", description: "Спать только в рекомендованной позиции"),
                (type: RestrictionType.Smoking, days: 2, reason: "Улучшение заживления", description: "Полный запрет на курение"),
                (type: RestrictionType.Alcohol, days: 10, reason: "Предотвращение осложнений", description: "Полный запрет на алкоголь"),
                (type: RestrictionType.Sports, days: 30, reason: "Защита трансплантатов", description: "Запрет на активный спорт, легкие нагрузки через месяц"),
                (type: RestrictionType.Headwear, days: 11, reason: "Защита пересаженных волос", description: "Не носить головные уборы"),
                (type: RestrictionType.HairDyeing, days: 240, reason: "Защита волосяных фолликулов", description: "Запрет на окрашивание волос"),
                (type: RestrictionType.Intimacy, days: 7, reason: "Предотвращение повышения давления", description: "Воздержание от интимной близости"),
                (type: RestrictionType.Sweating, days: 20, reason: "Защита от инфекций", description: "Избегать сильного потоотделения"),
                (type: RestrictionType.WaterActivities, days: 30, reason: "Предотвращение инфекций", description: "Запрет на бассейн, море, сауну, баню, солярий")
            };

            foreach (var restriction in restrictions)
            {
                events.Add(new RestrictionEvent
                {
                    Date = surgeryDate,
                    Type = EventType.Restriction,
                    RestrictionType = restriction.type,
                    Title = $"Ограничение: {restriction.type}",
                    Description = restriction.description,
                    EndDate = surgeryDate.AddDays(restriction.days),
                    Reason = restriction.reason,
                    IsRequired = true,
                    AlternativeSuggestion = GetAlternativeSuggestion(restriction.type)
                });
            }
        }

        private string GetAlternativeSuggestion(RestrictionType type)
        {
            return type switch
            {
                RestrictionType.BendingDown => "Используйте подставку для чтения, держите телефон на уровне глаз",
                RestrictionType.Sports => "Можно совершать легкие прогулки",
                RestrictionType.Headwear => "При необходимости защиты от солнца используйте зонт",
                _ => string.Empty
            };
        }

        private void AddPhotoReports(List<TreatmentEvent> events, DateTime surgeryDate)
        {
            // Ежедневные фото первые 10 дней
            for (int day = 1; day <= 10; day++)
            {
                events.Add(new PhotoReportEvent
                {
                    Date = surgeryDate.AddDays(day),
                    Type = EventType.PhotoReport,
                    Title = $"Фотоотчет: День {day}",
                    Description = "Сделайте фото зоны пересадки и донорской зоны",
                    RequiredAngles = new List<string> { "Фронтальный", "Левый бок", "Правый бок", "Затылок" },
                    IsRequired = true,
                    NeedsAttention = day == 10 // Особое внимание на 10-й день для проверки корочек
                });
            }

            // Ежемесячные фото
            for (int month = 1; month <= 12; month++)
            {
                events.Add(new PhotoReportEvent
                {
                    Date = surgeryDate.AddMonths(month),
                    Type = EventType.PhotoReport,
                    Title = $"Ежемесячный фотоотчет: {month} месяц",
                    Description = "Фото для отслеживания прогресса роста волос",
                    RequiredAngles = new List<string> { "Фронтальный", "Левый бок", "Правый бок", "Затылок" },
                    IsRequired = true
                });
            }
        }

        private void AddCareEvents(List<TreatmentEvent> events, DateTime surgeryDate)
        {
            // Инструкции по мытью головы (первые 10 дней)
            for (int day = 1; day <= 10; day++)
            {
                events.Add(new CareEvent
                {
                    Date = surgeryDate.AddDays(day),
                    Type = EventType.Care,
                    Title = "Мытье головы",
                    Description = "Особый режим мытья головы",
                    Steps = new List<string>
                    {
                        "Нанесите спрей на всю область, включая донорскую зону",
                        "Подождите 5-10 минут",
                        "Смойте прохладной водой без напора",
                        "Не трите зону пересадки",
                        "Высушите бумажным полотенцем, осторожно прикладывая его"
                    },
                    DurationMinutes = 20,
                    IsRequired = true,
                    VideoGuideUrl = "url_to_video_guide" // TODO: Добавить реальную ссылку
                });
            }

            // Регулярный уход после 10 дней
            for (int day = 11; day <= 365; day++)
            {
                events.Add(new CareEvent
                {
                    Date = surgeryDate.AddDays(day),
                    Type = EventType.Care,
                    Title = "Уход за волосами",
                    Description = "Использование специального шампуня",
                    IsRequired = true,
                    ProductToUse = "Специальный шампунь для восстановления"
                });
            }
        }

        private void AddMilestones(List<TreatmentEvent> events, DateTime surgeryDate)
        {
            var milestones = new[]
            {
                (days: 3, phase: "Начальное восстановление", symptoms: new[] { "Возможна тянущая, ноющая боль в зоне пересадки" }),
                (days: 4, phase: "Период заживления", symptoms: new[] { "Может появиться зуд в зоне пересадки", "Можно принимать антигистаминные" }),
                (days: 10, phase: "Удаление корочек", symptoms: new[] { "Корочки готовы к удалению", "Следуйте видеоинструкции" }),
                (days: 30, phase: "Шоковое выпадение", symptoms: new[] { "Начинается выпадение пересаженных волос - это нормально" }),
                (days: 90, phase: "Начало роста", symptoms: new[] { "Появление новых волос" }),
                (days: 180, phase: "Активный рост", symptoms: new[] { "Заметный рост новых волос" }),
                (days: 365, phase: "Финальный результат", symptoms: new[] { "Полноценный рост волос", "Создание коллажа до/после" })
            };

            foreach (var milestone in milestones)
            {
                events.Add(new MilestoneEvent
                {
                    Date = surgeryDate.AddDays(milestone.days),
                    Type = EventType.Milestone,
                    Title = milestone.phase,
                    Description = $"Этап восстановления: {milestone.phase}",
                    Phase = milestone.phase,
                    DaysSinceSurgery = milestone.days,
                    CommonSymptoms = new List<string>(milestone.symptoms),
                    IsRequired = false
                });
            }
        }

        private void AddPlasmaTherapy(List<TreatmentEvent> events, DateTime surgeryDate)
        {
            // Плазмотерапия каждый месяц в течение года
            for (int month = 1; month <= 12; month++)
            {
                events.Add(new PlasmaTherapyEvent
                {
                    Date = surgeryDate.AddMonths(month),
                    Type = EventType.PlasmaTherapy,
                    Title = $"Плазмотерапия: Сеанс {month}",
                    Description = "Рекомендуемый сеанс плазмотерапии",
                    SessionNumber = month,
                    IsRequired = false,
                    EstimatedCost = 15000, // Примерная стоимость
                    RecommendedClinics = new List<string> { "Клиника 1", "Клиника 2" }, // TODO: Добавить реальные клиники
                    PreparationInstructions = "Не принимать аспирин за 3 дня до процедуры"
                });
            }
        }
    }
} 