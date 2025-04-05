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
                    // Selected day - white text in dark theme, black text in light theme
                    var requestedTheme = Application.Current?.RequestedTheme ?? AppTheme.Light;
                    return requestedTheme == AppTheme.Dark 
                        ? Colors.White 
                        : Colors.Black;
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