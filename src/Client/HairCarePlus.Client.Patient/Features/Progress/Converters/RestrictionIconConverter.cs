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
            {"styling", "IconNoDye"},
            {"басс", "IconNoWater"},
            {"плаван", "IconNoWater"},
            {"вода", "IconNoWater"},
            {"душ", "IconNoWater"},
            {"солярий", "IconNoWater"}
        };

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Use full qualification to access MAUI Application's resources
            var resources = global::Microsoft.Maui.Controls.Application.Current?.Resources as global::Microsoft.Maui.Controls.ResourceDictionary;
            if (value is string title && resources != null)
            {
                // High-priority check: все ограничения, связанные с водой/плаванием
                var lower = title.ToLowerInvariant();
                if (lower.Contains("басс") || lower.Contains("плаван") || lower.Contains("вода") || lower.Contains("душ") || lower.Contains("солярий"))
                {
                    if (resources.TryGetValue("IconNoWater", out var waterRes) && waterRes is Microsoft.Maui.Controls.ImageSource waterImg)
                        return waterImg;
                }

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