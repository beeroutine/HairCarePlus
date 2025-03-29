using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace HairCarePlus.Client.Patient.Features.Calendar.Converters
{
    public class IsExpiredConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime eventDate && parameter is bool isCompleted)
            {
                // Проверяем, является ли событие просроченным (дата в прошлом и задача не выполнена)
                bool isExpired = eventDate.Date < DateTime.Today && !isCompleted;
                
                if (targetType == typeof(Color) || targetType == typeof(Microsoft.Maui.Graphics.Color))
                {
                    // Возвращаем соответствующий цвет
                    return isExpired ? Color.FromArgb("#F44336") : Colors.Transparent;
                }
                
                if (targetType == typeof(bool))
                {
                    return isExpired;
                }
                
                if (targetType == typeof(string))
                {
                    return isExpired ? "Expired" : string.Empty;
                }
            }
            
            if (targetType == typeof(Color) || targetType == typeof(Microsoft.Maui.Graphics.Color))
            {
                return Colors.Transparent;
            }
            
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 