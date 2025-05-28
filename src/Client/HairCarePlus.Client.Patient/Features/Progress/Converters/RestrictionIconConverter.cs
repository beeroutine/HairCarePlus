using System;
using System.Globalization;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;
using System.Text;

namespace HairCarePlus.Client.Patient.Features.Progress.Converters
{
    /// <summary>
    /// Конвертер для преобразования типа ограничения в имя файла PNG (MAUI конвертирует SVG в PNG во время сборки).
    /// </summary>
    public class RestrictionIconConverter : IValueConverter
    {
        // Альтернативные FontAwesome символы для fallback (могут быть не нужны, если SVG->PNG работает)
        private static readonly Dictionary<RestrictionIconType, string> FontAwesomeAlternatives = new()
        {
            { RestrictionIconType.NoSmoking, "\uf54d" }, 
            { RestrictionIconType.NoAlcohol, "\uf5c8" }, 
            { RestrictionIconType.NoSex, "\uf3c4" }, 
            { RestrictionIconType.NoHairCutting, "\uf2c7" }, 
            { RestrictionIconType.NoHatWearing, "\uf5c1" }, 
            { RestrictionIconType.NoStyling, "\uf5c3" }, 
            { RestrictionIconType.NoLaying, "\uf564" }, 
            { RestrictionIconType.NoSun, "\uf05e" }, 
            { RestrictionIconType.NoSweating, "\uf769" }, 
            { RestrictionIconType.NoSwimming, "\uf5c4" }, 
            { RestrictionIconType.NoSporting, "\uf70c" }, 
            { RestrictionIconType.NoTilting, "\uf3c4" }, 
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not RestrictionIconType iconType)
                return "no_smoking.png"; // fallback PNG filename (default)

            // MAUI ожидает, что SVG, объявленные как <MauiImage>, будут доступны в рантайме как PNG.
            // Поэтому строим имя ресурса в snake_case, чтобы совпадало с <LogicalName>="%(Filename).png" в csproj.
            string iconFileName = iconType switch
            {
                RestrictionIconType.NoAlcohol     => "no_alchohol.png",   // сохранён с опечаткой
                RestrictionIconType.NoHairCutting => "no_haircuting.png", // сохранён с опечаткой
                _ => $"{ToSnakeCase(iconType.ToString())}.png"
            };

            return iconFileName; // Возвращаем только имя файла с расширением .png
        }

        /// <summary>
        /// Получить FontAwesome символ для типа ограничения (запасной вариант)
        /// </summary>
        public static string GetFontAwesomeIcon(RestrictionIconType iconType)
        {
            return FontAwesomeAlternatives.TryGetValue(iconType, out var icon) ? icon : "\uf54d";
        }

        /// <summary>
        /// Преобразует PascalCase / CamelCase строку в snake_case.
        /// </summary>
        private static string ToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var sb = new StringBuilder(input.Length * 2);
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (char.IsUpper(c) && i > 0)
                    sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 