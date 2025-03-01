using HairCarePlus.Client.Patient.Features.Calendar.Models;
using System;
using System.Collections.Generic;

namespace HairCarePlus.Client.Patient.Features.Calendar.Data
{
    public class CalendarDataModel
    {
        public DateTime OperationDate { get; set; }
        public List<CalendarEvent> Events { get; set; }
    }
} 