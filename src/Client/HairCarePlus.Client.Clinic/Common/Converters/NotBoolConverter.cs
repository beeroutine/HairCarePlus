using System.Globalization;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Clinic.Common.Converters;

public sealed class NotBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}


