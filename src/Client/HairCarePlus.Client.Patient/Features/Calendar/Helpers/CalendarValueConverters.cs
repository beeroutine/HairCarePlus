using System;
using System.Collections;
using System.Globalization;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace HairCarePlus.Client.Patient.Features.Calendar.Helpers
{
    /// <summary>
    /// Converts a boolean value to one of two colors based on the converter parameter
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not bool boolValue || parameter is not string colorParams)
                return Colors.Transparent;

            string[] colors = colorParams.Split(',');
            if (colors.Length != 2)
                return Colors.Transparent;

            // If true, return the first color; if false, return the second color
            string colorStr = boolValue ? colors[0] : colors[1];
            
            return Color.FromArgb(colorStr);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts an EventType to a color
    /// </summary>
    public class EventTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not EventType eventType)
                return Colors.Gray;

            try
            {
                // Используем константы для ключей ресурсов вместо строковых литералов
                const string medicationColorKey = "MedicationColor";
                const string photoColorKey = "PhotoColor";
                const string restrictionColorKey = "RestrictionColor";
                const string instructionColorKey = "InstructionColor";
                const string defaultColorKey = "EventIndicatorColor";

                string resourceKey = eventType switch
                {
                    EventType.Medication => medicationColorKey,
                    EventType.Photo => photoColorKey,
                    EventType.Restriction => restrictionColorKey,
                    EventType.Instruction => instructionColorKey,
                    _ => defaultColorKey
                };
                
                if (Application.Current.Resources.TryGetValue(resourceKey, out var color) && color is Color)
                {
                    return (Color)color;
                }
                
                // Запасные цвета, если ресурсы не найдены
                return eventType switch
                {
                    EventType.Medication => Color.FromArgb("#2196F3"),
                    EventType.Photo => Color.FromArgb("#4CAF50"),
                    EventType.Restriction => Color.FromArgb("#F44336"),
                    EventType.Instruction => Color.FromArgb("#9C27B0"),
                    _ => Colors.Gray
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error converting EventType to Color: {ex.Message}");
                
                // Запасные цвета в случае исключения
                return eventType switch
                {
                    EventType.Medication => Color.FromArgb("#2196F3"),
                    EventType.Photo => Color.FromArgb("#4CAF50"),
                    EventType.Restriction => Color.FromArgb("#F44336"),
                    EventType.Instruction => Color.FromArgb("#9C27B0"),
                    _ => Colors.Gray
                };
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a double value to a percentage (0.0 to 1.0) for ProgressBar
    /// </summary>
    public class DoubleToPercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not double doubleValue)
                return 0.0;

            // Convert percentage (0-100) to progress (0-1)
            return Math.Min(1.0, Math.Max(0.0, doubleValue / 100.0));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not double doubleValue)
                return 0.0;

            // Convert progress (0-1) to percentage (0-100)
            return doubleValue * 100.0;
        }
    }

    /// <summary>
    /// Checks if a collection has items
    /// </summary>
    public class HasItemsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;
                
            if (value is ICollection collection)
            {
                return collection.Count > 0;
            }
            
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Проверяет, является ли событие типом отличным от Restriction
    /// </summary>
    public class IsNotRestrictionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EventType eventType)
            {
                return eventType != EventType.Restriction;
            }
            
            return true; // По умолчанию показываем чекбокс
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts an EventType to a background color for event cards
    /// </summary>
    public class EventTypeToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not EventType eventType)
                return Colors.Transparent;

            try
            {
                // Используем константы для ключей ресурсов фонов
                const string medicationColorKey = "MedicationBackgroundColor";
                const string photoColorKey = "PhotoBackgroundColor";
                const string restrictionColorKey = "RestrictionBackgroundColor";
                const string instructionColorKey = "InstructionBackgroundColor";
                const string defaultColorKey = "BackgroundColor";

                string resourceKey = eventType switch
                {
                    EventType.Medication => medicationColorKey,
                    EventType.Photo => photoColorKey,
                    EventType.Restriction => restrictionColorKey,
                    EventType.Instruction => instructionColorKey,
                    _ => defaultColorKey
                };
                
                if (Application.Current.Resources.TryGetValue(resourceKey, out var color) && color is Color)
                {
                    return (Color)color;
                }
                
                // Запасные цвета, если ресурсы не найдены
                return eventType switch
                {
                    EventType.Medication => Color.FromArgb("#182196F3"), 
                    EventType.Photo => Color.FromArgb("#184CAF50"),
                    EventType.Restriction => Color.FromArgb("#18F44336"),
                    EventType.Instruction => Color.FromArgb("#189C27B0"),
                    _ => Colors.Transparent
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error converting EventType to background Color: {ex.Message}");
                return Colors.Transparent;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 