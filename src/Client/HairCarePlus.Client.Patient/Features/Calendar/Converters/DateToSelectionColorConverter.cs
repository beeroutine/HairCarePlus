using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace HairCarePlus.Client.Patient.Features.Calendar.Converters
{
    public class DateToSelectionColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date && parameter is DateTime selectedDate)
            {
                if (date.Date == selectedDate.Date)
                {
                    return Application.Current.Resources["Primary"]; // Selected day - blue
                }
                else if (date.Date == DateTime.Today)
                {
                    return Application.Current.Resources["Gray200"]; // Today - light gray
                }
                return Colors.Transparent; // Other days - transparent
            }
            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 