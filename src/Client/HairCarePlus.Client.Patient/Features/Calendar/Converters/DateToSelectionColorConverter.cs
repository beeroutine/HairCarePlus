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
                    // Выбранный день - синий фон с большей непрозрачностью
                    return Application.Current.Resources["Primary"]; 
                }
                else if (date.Date == DateTime.Today && date.Date != selectedDate.Date)
                {
                    // Сегодняшний день (если не выбран) - светло-серый
                    return Application.Current.Resources["Gray200"]; 
                }
                return Colors.Transparent; // Другие дни - прозрачный фон
            }
            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 