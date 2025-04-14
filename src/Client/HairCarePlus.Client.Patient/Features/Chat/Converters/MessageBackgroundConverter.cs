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
            
            // Исправлено: всегда возвращать корректный цвет для patient/doctor, если targetType == Color
            if ((targetType == typeof(Microsoft.Maui.Graphics.Color)) || (parameter is string p && p.ToLower() == "backgroundcolor"))
            {
                if (senderId == "patient")
                {
                    return Application.Current?.RequestedTheme == AppTheme.Dark
                        ? Color.FromArgb("#1E2A35")
                        : Color.FromArgb("#EAF4FC");
                }
                else if (senderId == "doctor")
                {
                    return Application.Current?.RequestedTheme == AppTheme.Dark
                        ? Color.FromArgb("#4D7B63")
                        : Color.FromArgb("#A0DAB2");
                }
                // fallback: neutral color
                return Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#222222")
                    : Color.FromArgb("#F7F7F7");
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