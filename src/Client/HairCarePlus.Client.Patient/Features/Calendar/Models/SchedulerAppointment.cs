using System;
using Microsoft.Maui.Graphics;

namespace HairCarePlus.Client.Patient.Features.Calendar.Models
{
    public class CalendarAppointment
    {
        public string Subject { get; set; }
        public string Notes { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public Color Background { get; set; }
        public Color TextColor { get; set; }
        public bool IsAllDay { get; set; }
        public string RecurrenceRule { get; set; }
        public object Data { get; set; }
    }
} 