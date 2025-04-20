using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Models;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces
{
    public interface IHairTransplantEventGenerator
    {
        Task<IEnumerable<CalendarEvent>> GenerateEventsForPeriodAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<CalendarEvent>> GenerateMedicationEventsAsync(DateTime startDate);
        Task<IEnumerable<CalendarEvent>> GeneratePhotoEventsAsync(DateTime startDate);
        Task<IEnumerable<CalendarEvent>> GenerateVideoEventsAsync(DateTime startDate);
        Task<IEnumerable<CalendarEvent>> GenerateRestrictionEventsAsync(DateTime startDate);
        Task<IEnumerable<CalendarEvent>> GenerateRecommendationEventsAsync(DateTime startDate);
    }
} 