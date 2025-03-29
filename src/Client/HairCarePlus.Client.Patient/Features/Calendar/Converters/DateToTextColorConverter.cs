using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace HairCarePlus.Client.Patient.Features.Calendar.Converters
{
    public class DateToTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date && parameter is DateTime selectedDate)
            {
                if (date.Date == selectedDate.Date)
                {
                    return Colors.White; // Selected day - white text
                }
                else if (date.Date == DateTime.Today)
                {
                    return Application.Current.Resources["Primary"]; // Today - blue text
                }
                return Application.Current.Resources["Gray600"]; // Other days - dark gray
            }
            return Application.Current.Resources["Gray600"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 