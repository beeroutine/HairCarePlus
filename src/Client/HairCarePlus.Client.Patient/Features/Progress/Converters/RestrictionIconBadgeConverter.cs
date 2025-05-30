using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Progress.Converters;

/// <summary>
/// Конвертер для определения FontAwesome иконки по названию ограничения.
/// Возвращает Unicode символ FontAwesome для отображения в badge.
/// </summary>
public class RestrictionIconBadgeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string restrictionTitle)
            return "\uf05a"; // Default info icon

        // Приводим к нижнему регистру для сравнения
        var title = restrictionTitle.ToLowerInvariant();

        // Определяем иконку по ключевым словам в названии
        return title switch
        {
            var t when t.Contains("курен") || t.Contains("сигарет") || t.Contains("табак") => "\uf48d", // fa-smoking-ban
            var t when t.Contains("алкоголь") || t.Contains("пиво") || t.Contains("вино") || t.Contains("водка") => "\uf4e3", // fa-wine-glass
            var t when t.Contains("секс") || t.Contains("интим") || t.Contains("близост") => "\uf228", // fa-venus-mars 
            var t when t.Contains("басс") || t.Contains("плаван") || t.Contains("вода") || t.Contains("душ") => "\uf5c5", // fa-swimmer
            var t when t.Contains("спорт") || t.Contains("физическ") || t.Contains("нагрузк") || t.Contains("тренировк") => "\uf44b", // fa-dumbbell
            var t when t.Contains("солнц") || t.Contains("загар") || t.Contains("пляж") || t.Contains("солярий") => "\uf185", // fa-sun
            var t when t.Contains("мыт") || t.Contains("голов") || t.Contains("шампун") => "\uf2cc", // fa-bath
            var t when t.Contains("стрижк") || t.Contains("машинк") || t.Contains("брить") => "\uf0c4", // fa-cut/scissors
            var t when t.Contains("шляп") || t.Contains("шапк") || t.Contains("кепк") => "\uf8c0", // fa-hat-cowboy
            var t when t.Contains("наклон") || t.Contains("голов") || t.Contains("нагиб") => "\uf2a3", // fa-user-headset
            _ => "\uf05a" // fa-info-circle - дефолтная иконка
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("RestrictionIconBadgeConverter does not support ConvertBack");
    }
} 