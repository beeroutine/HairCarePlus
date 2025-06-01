using System;
using System.Globalization;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using HairCarePlus.Shared.Domain.Restrictions;
using System.Text;
using HairCarePlus.Client.Patient.Features.Progress.Services.Implementation;

namespace HairCarePlus.Client.Patient.Features.Progress.Converters
{
    /// <summary>
    /// Конвертер для преобразования типа ограничения в имя файла PNG (MAUI конвертирует SVG в PNG во время сборки).
    /// </summary>
    public class RestrictionIconConverter : IValueConverter
    {
        // Deprecated FontAwesome map kept for binary compatibility of static method GetFontAwesomeIcon
        private static readonly Dictionary<RestrictionIconType, string> _dummy = new(); // kept for static init

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not RestrictionIconType iconType)
                return "no_smoking.png";

            return Services.Implementation.RestrictionIconMapper.ToPng(iconType);
        }

        /// <summary>
        /// Получить FontAwesome символ для типа ограничения (запасной вариант)
        /// </summary>
        public static string GetFontAwesomeIcon(RestrictionIconType iconType) =>
            Services.Implementation.RestrictionIconMapper.ToFaGlyph(iconType);

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 