using System.Globalization;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Common.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public Color TrueColor { get; set; }
        public Color FalseColor { get; set; }

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool and true ? TrueColor : FalseColor;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 