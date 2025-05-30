using System.Collections.Generic;
using System.Linq;
using HairCarePlus.Client.Patient.Features.Calendar.Models;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services
{
    public interface IProgressCalculator
    {
        (double progress, int percentage) CalculateProgress(IReadOnlyCollection<CalendarEvent> events);
    }

    public sealed class ProgressCalculatorService : IProgressCalculator
    {
        public (double progress, int percentage) CalculateProgress(IReadOnlyCollection<CalendarEvent> events)
        {
            // Фильтруем только реальные задачи (не учитываем предупреждения)
            var tasks = events?.Where(e => e.EventType != EventType.CriticalWarning).ToList();
            // Если нет задач, прогресс 0%
            if (tasks == null || tasks.Count == 0)
                return (0.0, 0);

            int total = tasks.Count;
            int completed = tasks.Count(e => e.IsCompleted);
            double prog = (double)completed / total;
            return (prog, (int)(prog * 100));
        }
    }
} 