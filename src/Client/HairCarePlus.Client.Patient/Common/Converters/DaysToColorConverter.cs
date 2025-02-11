using System.Globalization;

namespace HairCarePlus.Client.Patient.Common.Converters
{
    public class DaysToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int days)
            {
                if (days <= 3)
                    return Colors.LightPink;
                if (days <= 7)
                    return Colors.LightYellow;
                return Colors.Transparent;
            }
            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 