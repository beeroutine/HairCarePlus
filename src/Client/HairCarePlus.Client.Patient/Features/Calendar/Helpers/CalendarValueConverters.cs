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

            return eventType switch
            {
                EventType.Medication => Color.FromArgb("#2196F3"), // Blue
                EventType.Photo => Color.FromArgb("#4CAF50"),      // Green
                EventType.Restriction => Color.FromArgb("#F44336"), // Red
                EventType.Instruction => Color.FromArgb("#FF9800"), // Orange
                _ => Colors.Gray
            };
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
} 