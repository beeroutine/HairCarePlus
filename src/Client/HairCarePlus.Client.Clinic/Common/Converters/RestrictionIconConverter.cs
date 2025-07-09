using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using HairCarePlus.Shared.Domain.Restrictions;
using HairCarePlus.Client.Clinic.Common.Services;

namespace HairCarePlus.Client.Clinic.Common.Converters;

/// <summary>
/// Converts RestrictionIconType enum value to image file name (.png) located in Resources.
/// </summary>
public class RestrictionIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not RestrictionIconType iconType)
            return "no_smoking.png";

        var png = RestrictionIconMapper.ToPng(iconType);

        // Попытаемся вернуть PNG — если такой ресурс отсутствует, MAUI просто нарисует пустоту.
        // Чтобы не оставлять пустое кольцо, параллельно возвращаем запасной FontImageSource;
        // он будет показан, если PNG не загрузится.
        return new ImageSource[]
        {
            png,
            new FontImageSource
            {
                Glyph = RestrictionIconMapper.ToFaGlyph(iconType),
                FontFamily = "MaterialIcons",
                Size = 24
            }
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
} 