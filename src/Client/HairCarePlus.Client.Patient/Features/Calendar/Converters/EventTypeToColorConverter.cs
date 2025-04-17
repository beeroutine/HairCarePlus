using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using HairCarePlus.Client.Patient.Features.Calendar.Models;

namespace HairCarePlus.Client.Patient.Features.Calendar.Converters
{
    public class EventTypeToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var defaultColor = Colors.Gray;
            
            if (value is not EventType eventType)
            {
                return defaultColor;
            }

            var resources = Microsoft.Maui.Controls.Application.Current?.Resources;
            if (resources == null)
            {
                System.Diagnostics.Debug.WriteLine("Warning: Application.Current or Resources is null in EventTypeToColorConverter.");
                return defaultColor;
            }

            string resourceKey = eventType switch
            {
                EventType.MedicationTreatment => "MedicationBlue",
                EventType.MedicalVisit => "AppointmentGreen",
                EventType.Photo => "PhotoPurple",
                EventType.Video => "VideoOrange",
                EventType.GeneralRecommendation => "GeneralGray",
                EventType.CriticalWarning => "CriticalRed",
                _ => string.Empty
            };

            if (eventType == EventType.CriticalWarning)
            {
                if (resources.TryGetValue("CriticalRed", out var criticalColorResource) && criticalColorResource is Color criticalColor)
                {
                    return criticalColor;
                }
                return Colors.Red;
            }

            if (!string.IsNullOrEmpty(resourceKey) && resources.TryGetValue(resourceKey, out var colorResource) && colorResource is Color color)
            {
                return color;
            }
            
            System.Diagnostics.Debug.WriteLine($"Warning: Color resource key \"{resourceKey}\" not found for EventType {eventType}.");
            return defaultColor;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 