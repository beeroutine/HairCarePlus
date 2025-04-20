using System.Globalization;

namespace HairCarePlus.Client.Patient.Common.Converters
{
    public class BoolToTextConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not bool boolValue || parameter is not string param)
                return string.Empty;

            var options = param.Split(',');
            if (options.Length != 2)
                return string.Empty;

            return boolValue ? options[0] : options[1];
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 