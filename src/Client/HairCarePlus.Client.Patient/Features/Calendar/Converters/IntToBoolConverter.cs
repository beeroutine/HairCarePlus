using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Calendar.Converters
{
    /// <summary>
    /// Converts an integer value to a boolean value.
    /// By default, returns true if the value is greater than 0.
    /// </summary>
    public class IntToBoolConverter : IValueConverter
    {
        /// <summary>
        /// The threshold value at which to convert to true.
        /// Default is 0 (any value > 0 returns true).
        /// </summary>
        public int Threshold { get; set; } = 0;
        
        /// <summary>
        /// Whether to invert the result.
        /// Default is false (values > threshold return true).
        /// </summary>
        public bool Invert { get; set; } = false;
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                bool result = intValue > Threshold;
                return Invert ? !result : result;
            }
            
            // If the value is null or not an integer, return false or its inverse if Invert is true
            return Invert;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This converter doesn't support converting back
            throw new NotImplementedException();
        }
    }
} 