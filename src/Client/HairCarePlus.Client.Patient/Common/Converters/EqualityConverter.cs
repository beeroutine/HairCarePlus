using System.Globalization;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Common.Converters
{
    public class EqualityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Если значение null, сравниваем parameter с null
            if (value == null)
                return parameter == null;
                
            // Сравниваем значение с параметром
            return value.ToString().Equals(parameter?.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Обратное преобразование не используется
            throw new NotImplementedException();
        }
    }
} 