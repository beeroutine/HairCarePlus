using HairCarePlus.Client.Patient.Features.Calendar.Data;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services
{
    public class PostOperationCalendarService : ICalendarService
    {
        private readonly DateTime _operationDate;

        public PostOperationCalendarService()
        {
            // Для демонстрации используем фиксированную дату операции
            // В реальном приложении это будет получено из профиля пациента
            _operationDate = DateTime.Now.AddDays(-14);
        }

        public DateTime GetOperationDate() => _operationDate;

        public int GetCurrentDay()
        {
            return (DateTime.Now - _operationDate).Days + 1;
        }

        public async Task<IEnumerable<CalendarEvent>> GetEventsAsync()
        {
            // Имитация асинхронного запроса к API
            await Task.Delay(100);
            return new List<CalendarEvent>(); // Placeholder
        }

        public async Task<IEnumerable<CalendarEvent>> GetEventsForDayAsync(int day)
        {
            await Task.Delay(100);
            return new List<CalendarEvent>(); // Placeholder
        }

        public async Task<IEnumerable<CalendarEvent>> GetEventsForDateAsync(DateTime date)
        {
            int day = (date - _operationDate).Days + 1;
            return await GetEventsForDayAsync(day);
        }

        public async Task<IEnumerable<MedicationEvent>> GetMedicationsForDayAsync(int day)
        {
            await Task.Delay(100);
            return new List<MedicationEvent>(); // Placeholder
        }

        public async Task<IEnumerable<Restriction>> GetRestrictionsForDayAsync(int day)
        {
            await Task.Delay(100); // Имитация задержки сети
            return PostOperationCalendarData.Restrictions
                .Where(r => r.StartDay <= day && (!r.EndDay.HasValue || r.EndDay.Value >= day));
        }

        public async Task<IEnumerable<InstructionEvent>> GetInstructionsForDayAsync(int day)
        {
            await Task.Delay(100); // Имитация задержки сети
            return PostOperationCalendarData.Instructions
                .Where(i => i.StartDay <= day && (!i.EndDay.HasValue || i.EndDay.Value >= day));
        }

        public async Task<IEnumerable<CalendarEvent>> GetWarningsForDayAsync(int day)
        {
            await Task.Delay(100); // Имитация задержки сети
            return PostOperationCalendarData.Warnings
                .Where(w => w.StartDay <= day && (!w.EndDay.HasValue || w.EndDay.Value >= day));
        }

        public RecoveryPhase GetCurrentPhase(int currentDay = 0)
        {
            if (currentDay == 0)
                currentDay = GetCurrentDay();
                
            if (currentDay <= 3)
                return RecoveryPhase.Initial;
            else if (currentDay <= 14)
                return RecoveryPhase.EarlyRecovery;
            else if (currentDay <= 30)
                return RecoveryPhase.Healing;
            else if (currentDay <= 90)
                return RecoveryPhase.Growth;
            else if (currentDay <= 270)
                return RecoveryPhase.Maturation;
            else
                return RecoveryPhase.Final;
        }

        public async Task<RecoveryPhase> GetCurrentPhaseAsync()
        {
            int currentDay = GetCurrentDay();
            return GetCurrentPhase(currentDay);
        }

        public double GetProgressPercentage(int currentDay = 0)
        {
            if (currentDay == 0)
                currentDay = GetCurrentDay();
                
            // Максимальное значение - полное восстановление за 180 дней
            const int totalDays = 180;
            
            // Ограничиваем значение процента от 0 до 100
            double percentage = Math.Min(100, (double)currentDay / totalDays * 100);
            return Math.Round(percentage, 0);
        }

        public async Task<double> GetProgressPercentageAsync()
        {
            int currentDay = GetCurrentDay();
            return GetProgressPercentage(currentDay);
        }

        public async Task<IEnumerable<PhaseProgress>> GetPhaseProgressAsync()
        {
            int currentDay = GetCurrentDay();
            
            var phases = new List<PhaseProgress>
            {
                new PhaseProgress
                {
                    Phase = RecoveryPhase.Initial,
                    Name = "Начальная фаза",
                    Description = "Первые дни после операции",
                    StartDay = 1,
                    EndDay = 3,
                    IsCompleted = currentDay > 3,
                    IsActive = currentDay >= 1 && currentDay <= 3,
                    Progress = CalculatePhaseProgress(currentDay, 1, 3)
                },
                new PhaseProgress
                {
                    Phase = RecoveryPhase.EarlyRecovery,
                    Name = "Ранняя фаза",
                    Description = "Первые две недели восстановления",
                    StartDay = 4,
                    EndDay = 14,
                    IsCompleted = currentDay > 14,
                    IsActive = currentDay >= 4 && currentDay <= 14,
                    Progress = CalculatePhaseProgress(currentDay, 4, 14)
                },
                new PhaseProgress
                {
                    Phase = RecoveryPhase.Healing,
                    Name = "Фаза заживления",
                    Description = "Начало роста новых волос",
                    StartDay = 15,
                    EndDay = 30,
                    IsCompleted = currentDay > 30,
                    IsActive = currentDay >= 15 && currentDay <= 30,
                    Progress = CalculatePhaseProgress(currentDay, 15, 30)
                },
                new PhaseProgress
                {
                    Phase = RecoveryPhase.Growth,
                    Name = "Фаза роста",
                    Description = "Укрепление и рост волос",
                    StartDay = 31,
                    EndDay = 90,
                    IsCompleted = currentDay > 90,
                    IsActive = currentDay >= 31 && currentDay <= 90,
                    Progress = CalculatePhaseProgress(currentDay, 31, 90)
                },
                new PhaseProgress
                {
                    Phase = RecoveryPhase.Maturation,
                    Name = "Фаза созревания",
                    Description = "Окончательное формирование результата",
                    StartDay = 91,
                    EndDay = 270,
                    IsCompleted = currentDay > 270,
                    IsActive = currentDay >= 91 && currentDay <= 270,
                    Progress = CalculatePhaseProgress(currentDay, 91, 270)
                },
                new PhaseProgress
                {
                    Phase = RecoveryPhase.Final,
                    Name = "Финальная фаза",
                    Description = "Окончательный результат",
                    StartDay = 271,
                    EndDay = 365,
                    IsCompleted = currentDay > 365,
                    IsActive = currentDay >= 271 && currentDay <= 365,
                    Progress = CalculatePhaseProgress(currentDay, 271, 365)
                }
            };
            
            return phases;
        }

        public async Task<IEnumerable<ExpectedChange>> GetExpectedChangesAsync()
        {
            int currentDay = GetCurrentDay();
            
            var changes = new List<ExpectedChange>
            {
                new ExpectedChange
                {
                    Name = "Заживление донорской зоны",
                    Description = "Полное заживление области, откуда были извлечены фолликулы",
                    ExpectedDay = 10,
                    IsCompleted = currentDay > 10
                },
                new ExpectedChange
                {
                    Name = "Выпадение пересаженных волос",
                    Description = "Временное выпадение пересаженных волос - нормальный процесс",
                    ExpectedDay = 21,
                    IsCompleted = currentDay > 21
                },
                new ExpectedChange
                {
                    Name = "Начало роста новых волос",
                    Description = "Появление первых новых волос в пересаженной области",
                    ExpectedDay = 90,
                    IsCompleted = currentDay > 90
                },
                new ExpectedChange
                {
                    Name = "50% результата",
                    Description = "Достижение примерно половины от окончательного результата",
                    ExpectedDay = 180,
                    IsCompleted = currentDay > 180
                },
                new ExpectedChange
                {
                    Name = "Финальный результат",
                    Description = "Полный результат трансплантации волос",
                    ExpectedDay = 365,
                    IsCompleted = currentDay > 365
                }
            };
            
            return changes;
        }

        public async Task<IEnumerable<Milestone>> GetMilestonesAsync()
        {
            int currentDay = GetCurrentDay();
            
            var milestones = new List<Milestone>
            {
                new Milestone
                {
                    Day = 1,
                    Name = "День операции",
                    Description = "Трансплантация волос завершена",
                    IsCompleted = currentDay >= 1,
                    UnlockedActivities = new[] { "Прием назначенных лекарств", "Отдых" }
                },
                new Milestone
                {
                    Day = 3,
                    Name = "Первый контрольный осмотр",
                    Description = "Проверка состояния после операции",
                    IsCompleted = currentDay >= 3,
                    UnlockedActivities = new[] { "Первое мытье головы по инструкции" }
                },
                new Milestone
                {
                    Day = 10,
                    Name = "Снятие корочек",
                    Description = "Корочки на трансплантатах отпадают естественным образом",
                    IsCompleted = currentDay >= 10,
                    UnlockedActivities = new[] { "Обычное мытье головы", "Легкие физические нагрузки" }
                },
                new Milestone
                {
                    Day = 30,
                    Name = "Месяц после операции",
                    Description = "Завершение периода шока волосяных фолликулов",
                    IsCompleted = currentDay >= 30,
                    UnlockedActivities = new[] { "Стрижка волос", "Умеренные физические нагрузки" }
                },
                new Milestone
                {
                    Day = 90,
                    Name = "Три месяца после операции",
                    Description = "Начало активного роста новых волос",
                    IsCompleted = currentDay >= 90,
                    UnlockedActivities = new[] { "Плавание", "Интенсивные физические нагрузки" }
                },
                new Milestone
                {
                    Day = 180,
                    Name = "Полгода после операции",
                    Description = "Значительное улучшение густоты волос",
                    IsCompleted = currentDay >= 180,
                    UnlockedActivities = new[] { "Окрашивание волос", "Все виды физической активности" }
                },
                new Milestone
                {
                    Day = 365,
                    Name = "Год после операции",
                    Description = "Финальный результат трансплантации",
                    IsCompleted = currentDay >= 365,
                    UnlockedActivities = new[] { "Полная свобода в уходе за волосами" }
                }
            };
            
            return milestones;
        }

        public DateTime GetDateForDay(int dayNumber)
        {
            return _operationDate.AddDays(dayNumber - 1);
        }

        public CalendarDataModel GetCalendarData()
        {
            return new CalendarDataModel
            {
                OperationDate = _operationDate,
                Events = new List<CalendarEvent>() // Placeholder
            };
        }

        private double CalculatePhaseProgress(int currentDay, int phaseStartDay, int phaseEndDay)
        {
            if (currentDay < phaseStartDay)
                return 0;
            
            if (currentDay > phaseEndDay)
                return 100;
            
            int phaseDuration = phaseEndDay - phaseStartDay + 1;
            int daysInPhase = currentDay - phaseStartDay + 1;
            
            return Math.Round((double)daysInPhase / phaseDuration * 100, 1);
        }

        public bool IsPhaseCompleted(RecoveryPhase phase)
        {
            int currentDay = GetCurrentDay();
            return phase switch
            {
                RecoveryPhase.Initial => currentDay > 3,
                RecoveryPhase.EarlyRecovery => currentDay > 14,
                RecoveryPhase.Healing => currentDay > 30,
                RecoveryPhase.Growth => currentDay > 90,
                RecoveryPhase.Maturation => currentDay > 270,
                RecoveryPhase.Final => currentDay > 365,
                _ => false
            };
        }

        public Dictionary<string, string> GetExpectedConditionsForPhase(RecoveryPhase phase)
        {
            return phase switch
            {
                RecoveryPhase.Initial => new Dictionary<string, string>
                {
                    { "Отек", "Возможен отек в области лба" },
                    { "Корочки", "Формирование корочек на трансплантатах" },
                    { "Боль", "Незначительная боль в донорской зоне" }
                },
                RecoveryPhase.EarlyRecovery => new Dictionary<string, string>
                {
                    { "Заживление", "Заживление донорской зоны" },
                    { "Корочки", "Отпадение корочек" },
                    { "Покраснение", "Уменьшение покраснения" }
                },
                RecoveryPhase.Healing => new Dictionary<string, string>
                {
                    { "Выпадение", "Временное выпадение пересаженных волос" },
                    { "Заживление", "Полное заживление донорской зоны" },
                    { "Чувствительность", "Нормализация чувствительности кожи головы" }
                },
                RecoveryPhase.Growth => new Dictionary<string, string>
                {
                    { "Рост", "Начало роста новых волос" },
                    { "Толщина", "Постепенное увеличение толщины волос" },
                    { "Плотность", "Увеличение плотности волос" }
                },
                RecoveryPhase.Maturation => new Dictionary<string, string>
                {
                    { "Рост", "Активный рост и укрепление волос" },
                    { "Текстура", "Улучшение текстуры волос" },
                    { "Результат", "Достижение 70-80% от окончательного результата" }
                },
                RecoveryPhase.Final => new Dictionary<string, string>
                {
                    { "Результат", "Окончательный результат трансплантации" },
                    { "Плотность", "Максимальная плотность волос" },
                    { "Внешний вид", "Естественный внешний вид волос" }
                },
                _ => new Dictionary<string, string>()
            };
        }
    }
} 