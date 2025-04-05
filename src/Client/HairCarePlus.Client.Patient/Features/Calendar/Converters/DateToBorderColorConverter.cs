using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace HairCarePlus.Client.Patient.Features.Calendar.Converters
{
    public class DateToBorderColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime currentDate && parameter is DateTime selectedDate)
            {
                if (currentDate.Date == selectedDate.Date)
                {
                    // Более яркий и насыщенный цвет для границы выбранной даты
                    return Application.Current.Resources["Primary"]; // Primary blue
                }
                else if (currentDate.Date == DateTime.Today && currentDate.Date != selectedDate.Date)
                {
                    // Возвращаем более нейтральный цвет для сегодняшнего дня (если он не выбран)
                    return Color.FromArgb("#BBDEFB"); // Light blue
                }
            }
            
            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 