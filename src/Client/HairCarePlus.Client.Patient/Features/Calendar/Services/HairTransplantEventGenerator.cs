using System;
using System.Collections.Generic;
using System.Linq;
using HairCarePlus.Client.Patient.Features.Calendar.Models;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services
{
    public interface IHairTransplantEventGenerator
    {
        /// <summary>
        /// Генерирует события календаря для заданной даты на основе плана восстановления
        /// </summary>
        List<CalendarEvent> GenerateEventsForDate(DateTime date);
        
        /// <summary>
        /// Генерирует события календаря для диапазона дат на основе плана восстановления
        /// </summary>
        List<CalendarEvent> GenerateEventsForDateRange(DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Устанавливает дату операции
        /// </summary>
        void SetTransplantDate(DateTime transplantDate);
    }
    
    public class HairTransplantEventGenerator : IHairTransplantEventGenerator
    {
        private DateTime _transplantDate;
        private int _eventIdCounter = 1;
        
        public HairTransplantEventGenerator()
        {
            // По умолчанию устанавливаем дату операции на вчерашний день
            _transplantDate = DateTime.Today.AddDays(-1);
        }
        
        public void SetTransplantDate(DateTime transplantDate)
        {
            _transplantDate = transplantDate;
        }
        
        public List<CalendarEvent> GenerateEventsForDate(DateTime date)
        {
            var events = new List<CalendarEvent>();
            
            // Вычисляем, какой день после операции
            int daysAfterTransplant = (int)(date.Date - _transplantDate.Date).TotalDays;
            
            // Если день до операции, возвращаем пустой список
            if (daysAfterTransplant < 0)
                return events;
                
            // Генерируем события в зависимости от текущего дня после операции
            if (daysAfterTransplant == 0) // День операции
            {
                events.AddRange(GenerateTransplantDayEvents(date));
            }
            else if (daysAfterTransplant >= 1 && daysAfterTransplant <= 7) // Первая неделя
            {
                events.AddRange(GenerateFirstWeekEvents(date, daysAfterTransplant));
            }
            else if (daysAfterTransplant >= 8 && daysAfterTransplant <= 14) // Вторая неделя
            {
                events.AddRange(GenerateSecondWeekEvents(date, daysAfterTransplant));
            }
            else if (daysAfterTransplant >= 15 && daysAfterTransplant <= 30) // Первый месяц
            {
                events.AddRange(GenerateFirstMonthEvents(date, daysAfterTransplant));
            }
            else if (daysAfterTransplant > 30) // После первого месяца
            {
                events.AddRange(GenerateLaterEvents(date, daysAfterTransplant));
            }
            
            return events;
        }
        
        public List<CalendarEvent> GenerateEventsForDateRange(DateTime startDate, DateTime endDate)
        {
            var events = new List<CalendarEvent>();
            
            var currentDate = startDate.Date;
            while (currentDate <= endDate.Date)
            {
                events.AddRange(GenerateEventsForDate(currentDate));
                currentDate = currentDate.AddDays(1);
            }
            
            return events;
        }
        
        #region Event Generation Logic
        
        private List<CalendarEvent> GenerateTransplantDayEvents(DateTime date)
        {
            var events = new List<CalendarEvent>();
            
            // Прием лекарств
            events.Add(new CalendarEvent
            {
                Id = _eventIdCounter++,
                Date = date.Add(new TimeSpan(9, 0, 0)),
                Title = "Принять антибиотик",
                Description = "Начать курс антибиотиков по схеме",
                EventType = EventType.MedicationTreatment,
                Priority = EventPriority.High,
                TimeOfDay = TimeOfDay.Morning
            });
            
            events.Add(new CalendarEvent
            {
                Id = _eventIdCounter++,
                Date = date.Add(new TimeSpan(14, 0, 0)),
                Title = "Принять обезболивающее",
                Description = "При необходимости",
                EventType = EventType.MedicationTreatment,
                Priority = EventPriority.Normal,
                TimeOfDay = TimeOfDay.Afternoon
            });
            
            // Фотоотчет
            events.Add(new CalendarEvent
            {
                Id = _eventIdCounter++,
                Date = date.Add(new TimeSpan(12, 0, 0)),
                Title = "Сделать первое фото",
                Description = "Фото зоны пересадки и донорской зоны",
                EventType = EventType.Photo,
                Priority = EventPriority.High,
                TimeOfDay = TimeOfDay.Afternoon
            });
            
            // Ограничения
            events.Add(new CalendarEvent
            {
                Id = _eventIdCounter++,
                Date = date,
                Title = "Не трогать зону пересадки",
                Description = "Не прикасаться, не расчесывать, не носить тесные головные уборы",
                EventType = EventType.CriticalWarning,
                Priority = EventPriority.Critical,
                TimeOfDay = TimeOfDay.Morning,
                EndDate = date.AddDays(10) // Ограничение на 10 дней
            });
            
            // Рекомендация
            events.Add(new CalendarEvent
            {
                Id = _eventIdCounter++,
                Date = date.Add(new TimeSpan(21, 0, 0)),
                Title = "Спать полусидя",
                Description = "Для снижения отеков и предотвращения контакта с зоной пересадки",
                EventType = EventType.GeneralRecommendation,
                Priority = EventPriority.High,
                TimeOfDay = TimeOfDay.Evening
            });
            
            return events;
        }
        
        private List<CalendarEvent> GenerateFirstWeekEvents(DateTime date, int daysAfterTransplant)
        {
            var events = new List<CalendarEvent>();
            
            // Прием лекарств - ежедневно в первую неделю
            events.Add(new CalendarEvent
            {
                Id = _eventIdCounter++,
                Date = date.Add(new TimeSpan(9, 0, 0)),
                Title = "Принять антибиотик",
                Description = $"День {daysAfterTransplant} курса антибиотиков",
                EventType = EventType.MedicationTreatment,
                Priority = EventPriority.High,
                TimeOfDay = TimeOfDay.Morning
            });
            
            if (daysAfterTransplant <= 3) // Первые 3 дня обезболивающие
            {
                events.Add(new CalendarEvent
                {
                    Id = _eventIdCounter++,
                    Date = date.Add(new TimeSpan(21, 0, 0)),
                    Title = "Принять обезболивающее",
                    Description = "Перед сном",
                    EventType = EventType.MedicationTreatment,
                    Priority = EventPriority.Normal,
                    TimeOfDay = TimeOfDay.Evening
                });
            }
            
            // Ежедневный фотоотчет
            events.Add(new CalendarEvent
            {
                Id = _eventIdCounter++,
                Date = date.Add(new TimeSpan(15, 0, 0)),
                Title = "Ежедневный фотоотчет",
                Description = "Снимать общие планы и макросъемку зоны пересадки",
                EventType = EventType.Photo,
                Priority = EventPriority.High,
                TimeOfDay = TimeOfDay.Afternoon
            });
            
            // Специфические события для дней 3-4 (дополнительно)
            if (daysAfterTransplant == 3 || daysAfterTransplant == 4)
            {
                events.Add(new CalendarEvent
                {
                    Id = _eventIdCounter++,
                    Date = date.Add(new TimeSpan(13, 0, 0)),
                    Title = "Возможен зуд",
                    Description = "Разрешено принять антигистаминные препараты",
                    EventType = EventType.GeneralRecommendation,
                    Priority = EventPriority.Normal,
                    TimeOfDay = TimeOfDay.Afternoon
                });
            }
            
            return events;
        }
        
        private List<CalendarEvent> GenerateSecondWeekEvents(DateTime date, int daysAfterTransplant)
        {
            var events = new List<CalendarEvent>();
            
            // Ежедневный фотоотчет (до 10 дня)
            if (daysAfterTransplant <= 10)
            {
                events.Add(new CalendarEvent
                {
                    Id = _eventIdCounter++,
                    Date = date.Add(new TimeSpan(15, 0, 0)),
                    Title = "Ежедневный фотоотчет",
                    Description = "Снимать общие планы и макросъемку зоны пересадки",
                    EventType = EventType.Photo,
                    Priority = EventPriority.High,
                    TimeOfDay = TimeOfDay.Afternoon
                });
            }
            
            // События для 10-го дня
            if (daysAfterTransplant == 10)
            {
                events.Add(new CalendarEvent
                {
                    Id = _eventIdCounter++,
                    Date = date.Add(new TimeSpan(10, 0, 0)),
                    Title = "Смывание корочек",
                    Description = "Посмотрите видео-инструкцию по смыванию корочек",
                    EventType = EventType.VideoInstruction,
                    Priority = EventPriority.High,
                    TimeOfDay = TimeOfDay.Morning
                });
                
                events.Add(new CalendarEvent
                {
                    Id = _eventIdCounter++,
                    Date = date.Add(new TimeSpan(12, 0, 0)),
                    Title = "Начать прием витаминов",
                    Description = "Для поддержки роста волос",
                    EventType = EventType.MedicationTreatment,
                    Priority = EventPriority.Normal,
                    TimeOfDay = TimeOfDay.Afternoon
                });
            }
            
            // События для 14-го дня
            if (daysAfterTransplant == 14)
            {
                events.Add(new CalendarEvent
                {
                    Id = _eventIdCounter++,
                    Date = date.Add(new TimeSpan(11, 0, 0)),
                    Title = "Контрольный осмотр",
                    Description = "Посещение врача для контрольного осмотра",
                    EventType = EventType.MedicalVisit,
                    Priority = EventPriority.High,
                    TimeOfDay = TimeOfDay.Morning
                });
                
                events.Add(new CalendarEvent
                {
                    Id = _eventIdCounter++,
                    Date = date.Add(new TimeSpan(18, 0, 0)),
                    Title = "Массаж головы",
                    Description = "Посмотрите видео-инструкцию по массажу головы",
                    EventType = EventType.VideoInstruction,
                    Priority = EventPriority.Normal,
                    TimeOfDay = TimeOfDay.Evening
                });
            }
            
            return events;
        }
        
        private List<CalendarEvent> GenerateFirstMonthEvents(DateTime date, int daysAfterTransplant)
        {
            var events = new List<CalendarEvent>();
            
            // Ограничение на физические нагрузки до 30 дня
            if (daysAfterTransplant == 15)
            {
                events.Add(new CalendarEvent
                {
                    Id = _eventIdCounter++,
                    Date = date,
                    Title = "Избегать физических нагрузок",
                    Description = "Ограничение активных физических нагрузок до конца 1-го месяца",
                    EventType = EventType.CriticalWarning,
                    Priority = EventPriority.High,
                    TimeOfDay = TimeOfDay.Morning,
                    EndDate = _transplantDate.AddDays(30)
                });
            }
            
            // Еженедельные фотоотчеты
            if (daysAfterTransplant % 7 == 0)
            {
                events.Add(new CalendarEvent
                {
                    Id = _eventIdCounter++,
                    Date = date.Add(new TimeSpan(15, 0, 0)),
                    Title = "Еженедельный фотоотчет",
                    Description = $"Неделя {daysAfterTransplant / 7} после операции",
                    EventType = EventType.Photo,
                    Priority = EventPriority.High,
                    TimeOfDay = TimeOfDay.Afternoon
                });
            }
            
            // Месячный отчет
            if (daysAfterTransplant == 30)
            {
                events.Add(new CalendarEvent
                {
                    Id = _eventIdCounter++,
                    Date = date.Add(new TimeSpan(10, 0, 0)),
                    Title = "Месячный осмотр",
                    Description = "Посещение врача для осмотра спустя 1 месяц",
                    EventType = EventType.MedicalVisit,
                    Priority = EventPriority.High,
                    TimeOfDay = TimeOfDay.Morning
                });
                
                events.Add(new CalendarEvent
                {
                    Id = _eventIdCounter++,
                    Date = date.Add(new TimeSpan(15, 0, 0)),
                    Title = "Месячный фотоотчет",
                    Description = "Фотографии для фиксации результатов через 1 месяц",
                    EventType = EventType.Photo,
                    Priority = EventPriority.Critical,
                    TimeOfDay = TimeOfDay.Afternoon
                });
                
                events.Add(new CalendarEvent
                {
                    Id = _eventIdCounter++,
                    Date = date.Add(new TimeSpan(13, 0, 0)),
                    Title = "Запись на плазмотерапию",
                    Description = "Запишитесь на первую процедуру плазмотерапии",
                    EventType = EventType.GeneralRecommendation,
                    Priority = EventPriority.Normal,
                    TimeOfDay = TimeOfDay.Afternoon
                });
            }
            
            return events;
        }
        
        private List<CalendarEvent> GenerateLaterEvents(DateTime date, int daysAfterTransplant)
        {
            var events = new List<CalendarEvent>();
            
            // Ежемесячные фотоотчеты
            if (daysAfterTransplant % 30 == 0)
            {
                int monthNumber = daysAfterTransplant / 30;
                
                events.Add(new CalendarEvent
                {
                    Id = _eventIdCounter++,
                    Date = date.Add(new TimeSpan(15, 0, 0)),
                    Title = $"{monthNumber}-месячный фотоотчет",
                    Description = $"Фотографии для отслеживания результатов спустя {monthNumber} месяцев",
                    EventType = EventType.Photo,
                    Priority = EventPriority.High,
                    TimeOfDay = TimeOfDay.Afternoon
                });
                
                // Для ключевых месяцев добавляем осмотр
                if (monthNumber == 3 || monthNumber == 6 || monthNumber == 12)
                {
                    events.Add(new CalendarEvent
                    {
                        Id = _eventIdCounter++,
                        Date = date.Add(new TimeSpan(10, 0, 0)),
                        Title = $"Осмотр ({monthNumber} месяцев)",
                        Description = $"Посещение врача для осмотра спустя {monthNumber} месяцев",
                        EventType = EventType.MedicalVisit,
                        Priority = EventPriority.High,
                        TimeOfDay = TimeOfDay.Morning
                    });
                }
                
                // Плазмотерапия каждые 2 месяца
                if (monthNumber % 2 == 0 && monthNumber <= 10)
                {
                    events.Add(new CalendarEvent
                    {
                        Id = _eventIdCounter++,
                        Date = date.Add(new TimeSpan(16, 0, 0)),
                        Title = "Плазмотерапия",
                        Description = $"Процедура плазмотерапии ({monthNumber} месяцев)",
                        EventType = EventType.MedicalVisit,
                        Priority = EventPriority.Normal,
                        TimeOfDay = TimeOfDay.Afternoon
                    });
                }
            }
            
            // События для 60-го дня (2 месяца)
            if (daysAfterTransplant == 60)
            {
                events.Add(new CalendarEvent
                {
                    Id = _eventIdCounter++,
                    Date = date.Add(new TimeSpan(9, 0, 0)),
                    Title = "Разрешена окраска волос",
                    Description = "Можно осторожно красить и укладывать волосы",
                    EventType = EventType.GeneralRecommendation,
                    Priority = EventPriority.Low,
                    TimeOfDay = TimeOfDay.Morning
                });
            }
            
            // События для 90-го дня (3 месяца)
            if (daysAfterTransplant == 90)
            {
                events.Add(new CalendarEvent
                {
                    Id = _eventIdCounter++,
                    Date = date.Add(new TimeSpan(9, 0, 0)),
                    Title = "Сняты все ограничения",
                    Description = "Разрешены все физические нагрузки и процедуры",
                    EventType = EventType.GeneralRecommendation,
                    Priority = EventPriority.Normal,
                    TimeOfDay = TimeOfDay.Morning
                });
            }
            
            // Годовщина операции
            if (daysAfterTransplant == 365)
            {
                events.Add(new CalendarEvent
                {
                    Id = _eventIdCounter++,
                    Date = date.Add(new TimeSpan(10, 0, 0)),
                    Title = "Годовой осмотр",
                    Description = "Итоговый осмотр у врача через 1 год после операции",
                    EventType = EventType.MedicalVisit,
                    Priority = EventPriority.Critical,
                    TimeOfDay = TimeOfDay.Morning
                });
                
                events.Add(new CalendarEvent
                {
                    Id = _eventIdCounter++,
                    Date = date.Add(new TimeSpan(15, 0, 0)),
                    Title = "Годовой фотоотчет",
                    Description = "Итоговые фотографии результатов через 1 год",
                    EventType = EventType.Photo,
                    Priority = EventPriority.Critical,
                    TimeOfDay = TimeOfDay.Afternoon
                });
                
                events.Add(new CalendarEvent
                {
                    Id = _eventIdCounter++,
                    Date = date.Add(new TimeSpan(16, 0, 0)),
                    Title = "Коллаж До и После",
                    Description = "Создание итогового коллажа с результатами за год",
                    EventType = EventType.VideoInstruction,
                    Priority = EventPriority.High,
                    TimeOfDay = TimeOfDay.Afternoon
                });
            }
            
            return events;
        }
        
        #endregion
    }
} 