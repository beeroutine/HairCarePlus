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
            {"курен", "IconSmoking"},
            {"smok", "IconSmoking"},
            {"алког", "IconAlcohol"},
            {"alcohol", "IconAlcohol"},
            {"спорт", "IconNoSport"},
            {"sport", "IconNoSport"},
            {"стрич", "IconNoHaircut"},
            {"haircut", "IconNoHaircut"},
            {"загар", "IconNoSun"},
            {"sun", "IconNoSun"},
            {"накло", "IconNoBendHead"},
            {"tilt", "IconNoBendHead"},
            {"сауна", "IconNoSweat"},
            {"sweat", "IconNoSweat"},
            {"swim", "IconNoWater"},
            {"hat", "IconNoHats"},
            {"sex", "IconNoSex"},
            {"styling", "IconNoDye"}
        };

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Use full qualification to access MAUI Application's resources
            var resources = global::Microsoft.Maui.Controls.Application.Current?.Resources as global::Microsoft.Maui.Controls.ResourceDictionary;
            if (value is string title && resources != null)
            {
                foreach (var kvp in _map)
                {
                    if (title.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        var key = kvp.Value;
                        if (resources.TryGetValue(key, out var res) && res is Microsoft.Maui.Controls.ImageSource img)
                            return img;
                    }
                }
            }
            // Fallback to smoking icon
            if (resources != null && resources.TryGetValue("IconSmoking", out var fb) && fb is Microsoft.Maui.Controls.ImageSource fbImg)
                return fbImg;
            return null!;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
} 