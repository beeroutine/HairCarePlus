using System.Globalization;

namespace HairCarePlus.Client.Patient.Features.Calendar.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isCurrentMonth)
            {
                return isCurrentMonth 
                    ? Application.Current.RequestedTheme == AppTheme.Dark 
                        ? Colors.White 
                        : Colors.Black
                    : Application.Current.RequestedTheme == AppTheme.Dark 
                        ? Colors.Gray 
                        : Colors.LightGray;
            }
            return Colors.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToBackgroundConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue 
                    ? Application.Current.RequestedTheme == AppTheme.Dark 
                        ? Color.FromArgb("#4A1515") // Темно-красный для темной темы
                        : Color.FromArgb("#FFEBEE") // Светло-красный для светлой темы
                    : Application.Current.RequestedTheme == AppTheme.Dark 
                        ? Color.FromArgb("#1E1E1E") // Темно-серый для темной темы
                        : Color.FromArgb("#F5F5F5"); // Светло-серый для светлой темы
            }
            return Application.Current.RequestedTheme == AppTheme.Dark 
                ? Color.FromArgb("#1E1E1E") 
                : Color.FromArgb("#F5F5F5");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringNotEmptyConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return !string.IsNullOrWhiteSpace(stringValue);
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IntToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                return intValue > 0;
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToBoldConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? FontAttributes.Bold : FontAttributes.None;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 