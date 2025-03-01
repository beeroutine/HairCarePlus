using HairCarePlus.Client.Patient.Features.Calendar.Data;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services
{
    public interface ICalendarService : IPostOperationCalendarService
    {
        // This interface inherits all methods from IPostOperationCalendarService
        // Additional methods specific to the new interface can be added here
    }
} 