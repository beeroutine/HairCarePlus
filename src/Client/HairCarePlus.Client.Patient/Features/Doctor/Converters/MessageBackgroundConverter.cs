using System.Globalization;

namespace HairCarePlus.Client.Patient.Features.Doctor.Converters;

public class MessageBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string senderId)
            return null;

        var isOutgoing = senderId == "patient";
        var paramString = parameter as string;

        // Handle avatar visibility
        if (paramString == "avatar")
            return !isOutgoing;

        // Handle message column placement
        if (paramString == "column")
            return isOutgoing ? 1 : 1;

        if (targetType == typeof(Color))
            return isOutgoing ? Color.FromArgb("#E7FFE3") : Colors.White;
        
        if (targetType == typeof(LayoutOptions))
            return isOutgoing ? LayoutOptions.End : LayoutOptions.Start;
        
        if (targetType == typeof(Thickness))
            return isOutgoing ? new Thickness(40, 2, 2, 2) : new Thickness(2, 2, 40, 2);

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 