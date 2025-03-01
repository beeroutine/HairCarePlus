using HairCarePlus.Client.Patient.Features.Calendar.Data;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services
{
    public interface IPostOperationCalendarService
    {
        DateTime GetOperationDate();
        int GetCurrentDay();
        DateTime GetDateForDay(int dayNumber);
        double GetProgressPercentage(int currentDay = 0);
        CalendarDataModel GetCalendarData();
        RecoveryPhase GetCurrentPhase(int currentDay = 0);
        bool IsPhaseCompleted(RecoveryPhase phase);
        Dictionary<string, string> GetExpectedConditionsForPhase(RecoveryPhase phase);
        
        // Асинхронные методы для получения данных календаря
        Task<IEnumerable<CalendarEvent>> GetEventsAsync();
        Task<IEnumerable<CalendarEvent>> GetEventsForDayAsync(int day);
        Task<IEnumerable<CalendarEvent>> GetEventsForDateAsync(DateTime date);
        Task<IEnumerable<MedicationEvent>> GetMedicationsForDayAsync(int day);
        Task<IEnumerable<Restriction>> GetRestrictionsForDayAsync(int day);
        Task<IEnumerable<InstructionEvent>> GetInstructionsForDayAsync(int day);
        Task<IEnumerable<CalendarEvent>> GetWarningsForDayAsync(int day);
        Task<RecoveryPhase> GetCurrentPhaseAsync();
        Task<double> GetProgressPercentageAsync();
        Task<IEnumerable<PhaseProgress>> GetPhaseProgressAsync();
        Task<IEnumerable<ExpectedChange>> GetExpectedChangesAsync();
        Task<IEnumerable<Milestone>> GetMilestonesAsync();
    }
} 