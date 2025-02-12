using System.Globalization;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Common.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                if (parameter is string colorName)
                {
                    return boolValue ? Color.FromArgb(colorName) : Colors.Gray;
                }
                return boolValue ? Colors.Green : Colors.Gray;
            }
            return Colors.Gray;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 