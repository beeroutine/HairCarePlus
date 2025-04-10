using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using HairCarePlus.Client.Patient.Features.Calendar.Models;

namespace HairCarePlus.Client.Patient.Features.Calendar.Converters
{
    public class EventTypeToLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EventType eventType)
            {
                return eventType switch
                {
                    EventType.MedicationTreatment => "MEDICATION",
                    EventType.MedicalVisit => "VISIT",
                    EventType.Photo => "PHOTO",
                    EventType.Video => "VIDEO",
                    EventType.GeneralRecommendation => "RECOMMENDATION",
                    EventType.CriticalWarning => "WARNING",
                    _ => "UNKNOWN"
                };
            }
            return "UNKNOWN";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 