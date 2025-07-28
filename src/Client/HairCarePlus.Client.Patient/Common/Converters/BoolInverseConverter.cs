using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Common.Converters
{
    /// <summary>
    /// Inverts a boolean value (true → false, false → true) for use in XAML bindings.
    /// </summary>
    public class BoolInverseConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return !(value is bool && (bool)value);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return !(value is bool && (bool)value);
        }
    }
} 