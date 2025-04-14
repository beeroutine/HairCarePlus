using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace HairCarePlus.Client.Patient.Features.Calendar.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not bool boolValue || parameter is not string options)
                return Colors.Transparent;

            var colors = options.Split('|');
            if (colors.Length != 2)
                return Colors.Transparent;

            return boolValue ? Color.Parse(colors[0]) : Color.Parse(colors[1]);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 