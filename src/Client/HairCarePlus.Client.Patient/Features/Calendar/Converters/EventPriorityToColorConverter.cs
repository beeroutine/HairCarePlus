using System;
using System.Globalization;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace HairCarePlus.Client.Patient.Features.Calendar.Converters
{
    public class EventPriorityToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is EventPriority priority)
            {
                switch (priority)
                {
                    case EventPriority.Critical:
                        return Color.FromArgb("#F44336"); // Красный
                    case EventPriority.High:
                        return Color.FromArgb("#FF9800"); // Оранжевый
                    case EventPriority.Normal:
                        return Color.FromArgb("#2196F3"); // Синий
                    case EventPriority.Low:
                        return Color.FromArgb("#4CAF50"); // Зеленый
                    default:
                        return Colors.Gray;
                }
            }
            
            return Colors.Gray;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 