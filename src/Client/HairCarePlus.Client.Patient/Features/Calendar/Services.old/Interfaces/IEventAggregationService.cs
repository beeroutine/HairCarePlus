using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Models;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces
{
    public interface IEventAggregationService
    {
        Task<List<CalendarEvent>> GetAllEventsForDateAsync(DateTime date);
    }
} 