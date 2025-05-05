using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace HairCarePlus.Client.Patient.Features.Calendar.Converters
{
    /// <summary>
    /// Converts a progress value (0–1) to a dash array "dash gap" that can be applied to an Ellipse border
    /// to visualise circular progress.
    /// </summary>
    public sealed class ProgressToDashArrayConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double progress && progress >= 0)
            {
                double diameter = 0;
                switch (parameter)
                {
                    case double d:
                        diameter = d;
                        break;
                    case string s when double.TryParse(s, out var parsed):
                        diameter = parsed;
                        break;
                }

                // Fallback diameter if not supplied (avoid division by 0)
                if (diameter <= 0)
                    diameter = 120; // reasonable default; adjust if needed

                var circumference = Math.PI * diameter;
                var dash = circumference * progress;
                var gap = Math.Max(circumference - dash, 0.1); // avoid 0 gap => visible full circle
                return new DoubleCollection { dash, gap };
            }
            return new DoubleCollection { 0, 1 };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }
} 