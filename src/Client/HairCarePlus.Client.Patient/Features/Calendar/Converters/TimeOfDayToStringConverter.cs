using System;
using System.Globalization;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Calendar.Converters
{
    public class TimeOfDayToStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is TimeOfDay timeOfDay)
            {
                switch (timeOfDay)
                {
                    case TimeOfDay.Morning:
                        return "Morning";
                    case TimeOfDay.Afternoon:
                        return "Afternoon";
                    case TimeOfDay.Evening:
                        return "Evening";
                    default:
                        return string.Empty;
                }
            }
            
            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 