using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using MauiApp = Microsoft.Maui.Controls.Application;

namespace HairCarePlus.Client.Patient.Features.Progress.Converters;

/// <summary>
/// Returns stroke color for restriction circle based on <see cref="int"/> days remaining.
/// • &gt; 1 day  – Primary  (active)
/// • 0-1 day   – SurfaceVariant (yellow accent)
/// • &lt;= 0     – Surface (gray, considered completed)
/// </summary>
public sealed class RestrictionStrokeColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int days)
        {
            bool isDark = MauiApp.Current?.RequestedTheme == AppTheme.Dark;

            // Completed restriction
            if (days <= 0)
                return GetColor(isDark ? "Gray600" : "Gray400", isDark ? Colors.DimGray : Colors.LightGray);

            // About to finish (warning)
            if (days == 1)
                return GetColor("Yellow100Accent", Color.FromArgb("#F7B548"));

            // Active
            return GetColor(isDark ? "PrimaryLight" : "Primary", Colors.DeepSkyBlue);
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