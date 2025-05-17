using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using MauiApp = Microsoft.Maui.Controls.Application;

namespace HairCarePlus.Client.Patient.Features.Progress.Converters;

/// <summary>
/// Returns background (fill) color for restriction circle based on days remaining.
/// • &gt; 1 day  – Transparent (only stroke visible)
/// • 0-1 day   – Warning accent (Yellow100Accent)
/// • &lt;= 0     – Completed (Surface / Gray tone)
/// </summary>
public sealed class RestrictionFillColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int days)
        {
            bool isDark = MauiApp.Current?.RequestedTheme == AppTheme.Dark;

            if (days <= 0)
                return GetColor(isDark ? "Gray600" : "Gray300", isDark ? Colors.DimGray : Colors.LightGray);

            if (days == 1)
                return GetColor("Yellow100Accent", Color.FromArgb("#F7B548"));

            // Active &gt;1 day – keep transparent to emphasise stroke only
            return Colors.Transparent;
        }

        return Colors.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();

    private static Color GetColor(string key, Color fallback)
    {
        if (MauiApp.Current?.Resources.TryGetValue(key, out var res) == true && res is Color c)
            return c;
        return fallback;
    }
} 