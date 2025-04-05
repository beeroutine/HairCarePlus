using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Calendar.Converters
{
    public class DateToIsSelectedConverter : IMultiValueConverter, IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime date && parameter is DateTime selectedDate)
            {
                bool isSelected = date.Date == selectedDate.Date;
                return isSelected ? 1.1 : 1.0; // По умолчанию делаем масштаб 1.1 для выбранной даты
            }
            
            return 1.0; // Для невыбранных дат масштаб по умолчанию
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not DateTime date || values[1] is not DateTime selectedDate)
                return 1.0;

            bool isSelected = date.Date == selectedDate.Date;
            
            // Если передан третий параметр с масштабом, используем его
            if (values.Length >= 3 && values[2] is double scaleValue)
                return isSelected ? scaleValue : 1.0;
                
            return isSelected ? 1.1 : 1.0; // По умолчанию 1.1 для выбранной даты
        }
        
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 