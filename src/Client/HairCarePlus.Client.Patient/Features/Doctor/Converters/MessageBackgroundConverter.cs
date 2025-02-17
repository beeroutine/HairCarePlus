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
            return isOutgoing ? 2 : 1;

        // Handle alignment
        if (paramString == "alignment")
            return isOutgoing ? LayoutOptions.End : LayoutOptions.Start;

        // Handle margin
        if (paramString == "margin")
            return isOutgoing ? new Thickness(80, 2, 8, 2) : new Thickness(44, 2, 80, 2);

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 