using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using MauiApp = Microsoft.Maui.Controls.Application;

namespace HairCarePlus.Client.Patient.Features.Calendar.Converters
{
    /// <summary>
    /// Converts remaining days of a restriction into stroke color of the circular badge.
    /// ≤ 3 days – accent warning red, otherwise neutral gray (light/dark theme aware).
    /// </summary>
    public sealed class DaysToStrokeColorConverter : IValueConverter
    {
        private static readonly Color WarningRed = Color.FromArgb("#F14336");

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int days)
            {
                if (days <= 3)
                    return WarningRed;

                // Neutral gray depending on theme
                return MauiApp.Current?.RequestedTheme == AppTheme.Dark
                    ? MauiApp.Current.Resources.TryGetValue("Gray600", out var dark) ? dark : Colors.Gray
                    : MauiApp.Current.Resources.TryGetValue("Gray400", out var light) ? light : Colors.LightGray;
            }
            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
} 