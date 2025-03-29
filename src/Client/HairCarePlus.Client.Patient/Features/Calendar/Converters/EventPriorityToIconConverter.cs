using System;
using System.Globalization;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Calendar.Converters
{
    public class EventPriorityToIconConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is EventPriority priority)
            {
                switch (priority)
                {
                    case EventPriority.Critical:
                        return "\ue002"; // Иконка priority_high из Material Icons
                    case EventPriority.High:
                        return "\ue047"; // Иконка arrow_upward из Material Icons
                    case EventPriority.Normal:
                        return "\ue889"; // Иконка drag_handle из Material Icons
                    case EventPriority.Low:
                        return "\ue046"; // Иконка arrow_downward из Material Icons
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