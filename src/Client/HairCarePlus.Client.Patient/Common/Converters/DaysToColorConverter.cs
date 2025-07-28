using System.Globalization;

namespace HairCarePlus.Client.Patient.Common.Converters
{
    public class DaysToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not int days)
                return Colors.Gray;

            return days switch
            {
                <= 0 => Colors.Red,
                <= 7 => Colors.Orange,
                _ => Colors.Green
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 