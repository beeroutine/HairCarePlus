using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Common.Converters;

/// <summary>
/// Converts a string (or its length) to a boolean indicating whether the "Read more" link should be visible.
/// </summary>
public sealed class ReadMoreVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Character count after which the Read&nbsp;more link becomes visible.
    /// Can be set in XAML: <converters:ReadMoreVisibilityConverter Threshold="120" />
    /// Defaults to 100.
    /// </summary>
    public int Threshold { get; set; } = 100;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string text)
            return false;

            return text.Length > Threshold;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 