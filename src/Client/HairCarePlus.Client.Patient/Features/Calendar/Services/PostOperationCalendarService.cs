using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services
{
    public interface IPostOperationCalendarService
    {
        DateTime OperationDate { get; }
        IEnumerable<CalendarEvent> GetEventsForDay(int dayNumber);
        IEnumerable<Restriction> GetActiveRestrictionsForDay(int dayNumber);
        IEnumerable<CalendarEvent> GetWarningsForDay(int dayNumber);
        IEnumerable<MedicationEvent> GetMedicationsForDay(int dayNumber);
        bool ShouldUploadPhotosOnDay(int dayNumber);
        IEnumerable<string> GetUnlockedActivitiesForDay(int dayNumber);
        int GetCurrentDay();
        DateTime GetDateForDay(int dayNumber);

        // New methods
        WashingInstructionEvent GetWashingInstructionsForDay(int dayNumber);
        ProgressCheckEvent GetProgressCheckForDay(int dayNumber);
        InstructionEvent GetInstructionsForDay(int dayNumber);
        RecoveryPhase GetCurrentPhase();
        Dictionary<string, string> GetExpectedConditionsForPhase(RecoveryPhase phase);
        IEnumerable<string> GetActiveWarningSignals(int dayNumber);
        bool IsPhaseCompleted(RecoveryPhase phase);
        double GetProgressPercentage();
    }

    public class PostOperationCalendarService : IPostOperationCalendarService
    {
        public DateTime OperationDate { get; }

        public PostOperationCalendarService(DateTime operationDate)
        {
            OperationDate = operationDate;
        }

        public IEnumerable<CalendarEvent> GetEventsForDay(int dayNumber)
        {
            return PostOperationCalendarData.Events
                .Where(e => IsEventActiveOnDay(e, dayNumber));
        }

        public IEnumerable<Restriction> GetActiveRestrictionsForDay(int dayNumber)
        {
            return PostOperationCalendarData.Restrictions
                .Where(r => IsEventActiveOnDay(r, dayNumber));
        }

        public IEnumerable<CalendarEvent> GetWarningsForDay(int dayNumber)
        {
            return PostOperationCalendarData.Warnings
                .Where(w => IsEventActiveOnDay(w, dayNumber));
        }

        public IEnumerable<MedicationEvent> GetMedicationsForDay(int dayNumber)
        {
            return GetEventsForDay(dayNumber)
                .OfType<MedicationEvent>();
        }

        public bool ShouldUploadPhotosOnDay(int dayNumber)
        {
            return GetEventsForDay(dayNumber)
                .OfType<PhotoUploadEvent>()
                .Any();
        }

        public IEnumerable<string> GetUnlockedActivitiesForDay(int dayNumber)
        {
            return GetEventsForDay(dayNumber)
                .OfType<MilestoneEvent>()
                .SelectMany(m => m.UnlockedActivities ?? Array.Empty<string>());
        }

        public int GetCurrentDay()
        {
            return (int)(DateTime.Now - OperationDate).TotalDays;
        }

        public DateTime GetDateForDay(int dayNumber)
        {
            return OperationDate.AddDays(dayNumber);
        }

        public WashingInstructionEvent GetWashingInstructionsForDay(int dayNumber)
        {
            return PostOperationCalendarData.WashingInstructions
                .FirstOrDefault(w => IsEventActiveOnDay(w, dayNumber));
        }

        public ProgressCheckEvent GetProgressCheckForDay(int dayNumber)
        {
            return PostOperationCalendarData.ProgressChecks
                .FirstOrDefault(p => IsEventActiveOnDay(p, dayNumber));
        }

        public InstructionEvent GetInstructionsForDay(int dayNumber)
        {
            return PostOperationCalendarData.Instructions
                .FirstOrDefault(i => IsEventActiveOnDay(i, dayNumber));
        }

        public RecoveryPhase GetCurrentPhase()
        {
            var currentDay = GetCurrentDay();
            
            if (currentDay <= 3) return RecoveryPhase.Initial;
            if (currentDay <= 10) return RecoveryPhase.EarlyRecovery;
            if (currentDay <= 30) return RecoveryPhase.Healing;
            if (currentDay <= 90) return RecoveryPhase.Growth;
            if (currentDay <= 240) return RecoveryPhase.Development;
            return RecoveryPhase.Final;
        }

        public Dictionary<string, string> GetExpectedConditionsForPhase(RecoveryPhase phase)
        {
            var progressCheck = PostOperationCalendarData.ProgressChecks
                .FirstOrDefault(p => p.Phase == phase);

            return progressCheck?.NormalConditions ?? new Dictionary<string, string>();
        }

        public IEnumerable<string> GetActiveWarningSignals(int dayNumber)
        {
            var progressCheck = GetProgressCheckForDay(dayNumber);
            if (progressCheck == null) return Array.Empty<string>();

            return progressCheck.WarningSignals.Select(w => $"{w.Key}: {w.Value}");
        }

        public bool IsPhaseCompleted(RecoveryPhase phase)
        {
            var currentDay = GetCurrentDay();
            return phase switch
            {
                RecoveryPhase.Initial => currentDay > 3,
                RecoveryPhase.EarlyRecovery => currentDay > 10,
                RecoveryPhase.Healing => currentDay > 30,
                RecoveryPhase.Growth => currentDay > 90,
                RecoveryPhase.Development => currentDay > 240,
                RecoveryPhase.Final => currentDay > 365,
                _ => false
            };
        }

        public double GetProgressPercentage()
        {
            var currentDay = GetCurrentDay();
            var totalDays = 365.0; // Полный период восстановления
            return Math.Min(100.0, (currentDay / totalDays) * 100.0);
        }

        private bool IsEventActiveOnDay(CalendarEvent ev, int dayNumber)
        {
            if (ev.IsRepeating && ev.RepeatIntervalDays.HasValue)
            {
                if (dayNumber < ev.StartDay) return false;
                if (ev.EndDay.HasValue && dayNumber > ev.EndDay.Value) return false;

                var daysSinceStart = dayNumber - ev.StartDay;
                return daysSinceStart % ev.RepeatIntervalDays.Value == 0;
            }

            return dayNumber >= ev.StartDay && 
                   (!ev.EndDay.HasValue || dayNumber <= ev.EndDay.Value);
        }
    }
} 