using System;
using System.Globalization;
using System.Collections.Generic;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Progress.Converters
{
    /// <summary>
    /// Maps RestrictionTimer.Title to an icon file name in Resources/AppIcon.
    /// Fallback returns a default prohibition icon.
    /// </summary>
    public sealed class RestrictionIconConverter : IValueConverter
    {
        private static readonly Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase)
        {
            {"курен", "no_smoking_black_300.png"},
            {"smok", "no_smoking_black_300.png"},
            {"алког", "no_alcohol_black_300.png"},
            {"alcohol", "no_alcohol_black_300.png"},
            {"спорт", "no_sport_black_300.png"},
            {"sport", "no_sport_black_300.png"},
            {"стрич", "no_haircut_black_300.png"},
            {"haircut", "no_haircut_black_300.png"},
            {"загар", "no_sun_black_300.png"},
            {"sun", "no_sun_black_300.png"},
            {"накло", "no_head_tilt_black_300.png"},
            {"tilt", "no_head_tilt_black_300.png"},
            {"сауна", "no_sweating_black_300.png"},
            {"sweat", "no_sweating_black_300.png"},
            {"swim", "no_swimming_black_300.png"},
            {"hat", "no_hat_black_300.png"},
            {"sex", "no_sex_black_300.png"},
            {"styling", "no_styling_black_300.png"}
        };

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string title)
            {
                foreach (var kvp in _map)
                {
                    if (title.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                        return kvp.Value;
                }
            }
            // Fallback prohibition icon
            return "no_horizontal_black_300.png";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
} 