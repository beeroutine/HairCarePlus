using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using HairCarePlus.Client.Patient.Features.Calendar.Models;

namespace HairCarePlus.Client.Patient.Features.Calendar.Converters
{
    public class EventTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EventType eventType)
            {
                return eventType switch
                {
                    EventType.Medication => Application.Current.Resources["MedicationEventColor"],
                    EventType.Photo => Application.Current.Resources["PhotoEventColor"],
                    EventType.Restriction => Application.Current.Resources["RestrictionEventColor"],
                    EventType.Instruction => Application.Current.Resources["InstructionEventColor"],
                    _ => Colors.Gray
                };
            }
            
            return Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 