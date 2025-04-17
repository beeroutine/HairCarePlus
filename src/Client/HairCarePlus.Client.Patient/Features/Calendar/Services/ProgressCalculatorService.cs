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
            if (events == null || events.Count == 0)
                return (0, 0);

            int total = events.Count;
            int completed = events.Count(e => e.IsCompleted);
            double prog = (double)completed / total;
            return (prog, (int)(prog * 100));
        }
    }
} 