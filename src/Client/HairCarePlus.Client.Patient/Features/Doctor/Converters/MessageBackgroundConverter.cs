using System.Globalization;

namespace HairCarePlus.Client.Patient.Features.Doctor.Converters;

public class MessageBackgroundConverter : IValueConverter
{
    private static readonly Thickness OutgoingMargin = new(80.0, 2.0, 8.0, 2.0);
    private static readonly Thickness IncomingMargin = new(8.0, 2.0, 80.0, 2.0);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return null;

        var senderId = value.ToString();
        var paramString = parameter.ToString();
        
        if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(paramString))
            return null;

        var isOutgoing = senderId == "patient";

        return paramString switch
        {
            "column" => isOutgoing ? 1 : 0,
            "alignment" => isOutgoing ? LayoutOptions.End : LayoutOptions.Start,
            "margin" => isOutgoing ? OutgoingMargin : IncomingMargin,
            _ => null
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 