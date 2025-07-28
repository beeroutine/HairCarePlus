using Microsoft.Maui.Controls;
using System.Globalization;

namespace HairCarePlus.Client.Clinic.Common.Converters;

public class BoolToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var texts = (parameter as string)?.Split(',');
        if (texts == null || texts.Length < 2) return value;
        return value is bool b && b ? texts[0] : texts[1];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
} 