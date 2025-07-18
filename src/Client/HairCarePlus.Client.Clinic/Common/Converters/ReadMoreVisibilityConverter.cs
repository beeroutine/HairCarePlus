using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Clinic.Common.Converters;

/// <summary>
/// Converts a string (or its length) to a boolean indicating whether the "Read more" link should be visible.
/// Mirrors implementation from Patient app to keep behavior consistent.
/// </summary>
public sealed class ReadMoreVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Character threshold after which the Read&nbsp;more link becomes visible.
    /// Defaults to <c>120</c>.
    /// </summary>
    public int Threshold { get; set; } = 120;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text)
            return text.Length > Threshold;

        if (value is int length)
            return length > Threshold;

        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
} 