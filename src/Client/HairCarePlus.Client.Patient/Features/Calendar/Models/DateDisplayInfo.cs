using System;

namespace HairCarePlus.Client.Patient.Features.Calendar.Models
{
    /// <summary>
    /// Группирует все свойства отображения даты для уменьшения количества OnPropertyChanged вызовов
    /// </summary>
    public class DateDisplayInfo
    {
        public string FormattedSelectedDate { get; set; } = string.Empty;
        public string CurrentMonthName { get; set; } = string.Empty;
        public string CurrentYear { get; set; } = string.Empty;
        public int DaysSinceTransplant { get; set; }
        public string DaysSinceTransplantSubtitle { get; set; } = string.Empty;
    }
} 