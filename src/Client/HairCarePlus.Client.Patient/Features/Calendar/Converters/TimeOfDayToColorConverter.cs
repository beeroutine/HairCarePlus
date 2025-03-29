using System;
using System.Globalization;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace HairCarePlus.Client.Patient.Features.Calendar.Converters
{
    public class TimeOfDayToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is TimeOfDay timeOfDay)
            {
                switch (timeOfDay)
                {
                    case TimeOfDay.Morning:
                        return Color.FromArgb("#FFA726"); // Оранжевый, как рассвет
                    case TimeOfDay.Afternoon:
                        return Color.FromArgb("#42A5F5"); // Голубой, как дневное небо
                    case TimeOfDay.Evening:
                        return Color.FromArgb("#5E35B1"); // Фиолетовый, как сумерки
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