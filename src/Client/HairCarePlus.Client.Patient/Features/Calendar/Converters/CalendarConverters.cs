using System.Globalization;

namespace HairCarePlus.Client.Patient.Features.Calendar.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = value is bool b && b;
            
            // Handle parameter string with format "TrueLight|TrueDark|FalseLight|FalseDark"
            if (parameter is string strParam && strParam.Contains('|'))
            {
                string[] parameters = strParam.Split('|');
                bool isDarkTheme = Application.Current.RequestedTheme == AppTheme.Dark;
                
                if (parameters.Length == 2) 
                {
                    // Format: "TrueColor,FalseColor"
                    return boolValue ? GetColorFromString(parameters[0]) : GetColorFromString(parameters[1]);
                }
                else if (parameters.Length == 4) 
                {
                    // Format: "TrueLight|TrueDark|FalseLight|FalseDark"
                    if (boolValue)
                    {
                        return isDarkTheme ? GetColorFromString(parameters[1]) : GetColorFromString(parameters[0]);
                    }
                    else
                    {
                        return isDarkTheme ? GetColorFromString(parameters[3]) : GetColorFromString(parameters[2]);
                    }
                }
            }
            
            // Default behavior
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

        private Color GetColorFromString(string colorString)
        {
            if (string.IsNullOrEmpty(colorString)) return Colors.Transparent;
            
            if (colorString.StartsWith("#"))
            {
                try
                {
                    return Color.FromArgb(colorString);
                }
                catch
                {
                    return Colors.Transparent;
                }
            }
            
            // Handle named colors
            switch (colorString.ToLowerInvariant())
            {
                case "white": return Colors.White;
                case "black": return Colors.Black;
                case "gray": return Colors.Gray;
                case "lightgray": return Colors.LightGray;
                case "transparent": return Colors.Transparent;
                case "primary": return Application.Current.RequestedTheme == AppTheme.Dark 
                                ? Color.FromArgb("#6962AD") : Color.FromArgb("#6962AD");
                case "secondary": return Application.Current.RequestedTheme == AppTheme.Dark 
                                ? Color.FromArgb("#504C8A") : Color.FromArgb("#A79FF7");
                default: return Colors.Transparent;
            }
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