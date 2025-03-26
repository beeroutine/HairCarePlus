using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Calendar.Helpers
{
    public class BoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Если передан параметр, инвертируем значение
                if (parameter is string strParam && strParam.ToLowerInvariant() == "false")
                {
                    return !boolValue;
                }
                
                return boolValue;
            }
            
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Если передан параметр, инвертируем значение
                if (parameter is string strParam && strParam.ToLowerInvariant() == "false")
                {
                    return !boolValue;
                }
                
                return boolValue;
            }
            
            return false;
        }
    }
} 