using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Calendar.Helpers
{
    /// <summary>
    /// Converts a double value (0–1) to a percentage string (e.g., 0.42 → "42%"), rounding to the nearest whole percent.
    /// </summary>
    public sealed class DoubleToPercentageConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                var percent = Math.Round(d * 100);
                return $"{percent}%";
            }
            return "0%";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    /// <summary>
    /// Returns true when the supplied IEnumerable (or Collection) contains at least one item.
    /// </summary>
    public sealed class HasItemsConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                System.Collections.IEnumerable enumerable => enumerable.GetEnumerator().MoveNext(),
                _ => false
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }
} 