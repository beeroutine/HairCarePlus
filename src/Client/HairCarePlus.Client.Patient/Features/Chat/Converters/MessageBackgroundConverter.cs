using System.Globalization;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Chat.Converters;

public class MessageBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string senderId)
        {
            // Логирование для отладки
            System.Diagnostics.Debug.WriteLine($"=== MessageBackgroundConverter.Convert: senderId={senderId}, targetType={targetType.Name}, parameter={parameter}");
            
            // Если параметр не указан, и тип назначения - Color, преобразуем SenderId в цвет
            if (parameter == null && targetType == typeof(Microsoft.Maui.Graphics.Color))
            {
                if (senderId == "patient")
                {
                    // Patient message colors
                    return Application.Current?.RequestedTheme == AppTheme.Dark 
                        ? Color.FromArgb("#1E2A35") // Dark blue for dark theme
                        : Color.FromArgb("#EAF4FC"); // Light blue for light theme
                }
                else
                {
                    // Doctor message colors
                    return Application.Current?.RequestedTheme == AppTheme.Dark 
                        ? Color.FromArgb("#4D7B63") // Dark green for dark theme
                        : Color.FromArgb("#A0DAB2"); // Light green for light theme
                }
            }
            
            if (parameter is string conversionType)
            {
                switch (conversionType.ToLower())
                {
                    case "column":
                        return senderId == "patient" ? 1 : 0;
                    case "alignment":
                        return senderId == "patient" ? LayoutOptions.End : LayoutOptions.Start;
                    case "margin":
                        return senderId == "patient" ? new Thickness(80, 0, 8, 0) : new Thickness(8, 0, 80, 0);
                    default:
                        return null;
                }
            }
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 