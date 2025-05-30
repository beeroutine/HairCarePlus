using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Common.Converters;

/// <summary>
/// Returns <c>true</c> when a numeric value (int) is greater than zero; otherwise <c>false</c>.
/// Useful for IsVisible bindings based on collection Count.
/// </summary>
public sealed class CountToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int i)
            return i > 0;
        if (value is long l)
            return l > 0;
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
} 