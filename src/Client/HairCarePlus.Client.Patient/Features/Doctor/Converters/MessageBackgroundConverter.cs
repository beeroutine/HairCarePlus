using System.Globalization;

namespace HairCarePlus.Client.Patient.Features.Doctor.Converters;

public class MessageBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isSending = (bool)value;
        return isSending ? Colors.LightBlue : Colors.LightGray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 