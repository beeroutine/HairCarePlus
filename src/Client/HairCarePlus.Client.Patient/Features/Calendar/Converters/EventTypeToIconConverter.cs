using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using HairCarePlus.Client.Patient.Features.Calendar.Models;

namespace HairCarePlus.Client.Patient.Features.Calendar.Converters
{
    public class EventTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EventType eventType)
            {
                return eventType switch
                {
                    EventType.MedicationTreatment => "\ue855", // medication
                    EventType.MedicalVisit => "\ue8f9",        // medical_services
                    EventType.Photo => "\ue412",               // photo_camera
                    EventType.Video => "\ue04b",               // play_circle
                    EventType.GeneralRecommendation => "\ue88e", // info
                    EventType.CriticalWarning => "\ue002",     // warning
                    _ => "\ue88e" // info
                };
            }
            return "\ue88e"; // info
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 