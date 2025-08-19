using Microsoft.Maui.Controls;
using System.Globalization;

namespace HairCarePlus.Client.Clinic.Common.Converters;

/// <summary>
/// Returns true when the bound value does NOT equal the BindingContext of the element
/// provided as ConverterParameter (e.g., a ContentView instance).
/// </summary>
public sealed class IsNotSameItemConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is Element el)
        {
            return !Equals(value, el.BindingContext);
        }
        return !Equals(value, parameter);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}


