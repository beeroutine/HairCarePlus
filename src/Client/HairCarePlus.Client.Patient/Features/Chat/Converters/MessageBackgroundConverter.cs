using System.Globalization;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Chat.Converters;

public class MessageBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string senderId && parameter is string conversionType)
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
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 