using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Progress.Converters;

/// <summary>
/// Конвертер для создания коротких фраз ограничений.
/// Преобразует полное название в краткую форму (не курить, не пить и т.д.)
/// </summary>
public class RestrictionShortPhraseConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string restrictionTitle)
            return "Не нарушать";

        // Приводим к нижнему регистру для сравнения
        var title = restrictionTitle.ToLowerInvariant();

        // Создаем короткие фразы
        return title switch
        {
            var t when t.Contains("курен") || t.Contains("сигарет") || t.Contains("табак") => "Не курить",
            var t when t.Contains("алкоголь") || t.Contains("пиво") || t.Contains("вино") || t.Contains("водка") => "Не пить",
            var t when t.Contains("секс") || t.Contains("интим") || t.Contains("близост") => "Воздержание",
            var t when t.Contains("спорт") || t.Contains("физическ") || t.Contains("нагрузк") || t.Contains("тренировк") => "Не заниматься спортом",
            var t when t.Contains("басс") || t.Contains("плаван") || t.Contains("вода") || t.Contains("душ") => "Не плавать",
            var t when t.Contains("стрижк") || t.Contains("машинк") || t.Contains("брить") => "Не стричься",
            var t when t.Contains("шляп") || t.Contains("шапк") || t.Contains("кепк") => "Без головных уборов",
            var t when t.Contains("наклон") || t.Contains("голов") || t.Contains("нагиб") => "Не наклонять голову",
            var t when t.Contains("лежать") || t.Contains("спать") || t.Contains("сон") => "Не лежать",
            var t when t.Contains("активн") => "Избегать активности",
            var t when t.Contains("интенсив") => "Избегать интенсивности",
            var t when t.Contains("солнц") || t.Contains("загар") || t.Contains("пляж") || t.Contains("солярий") => "Не загорать",
            var t when t.Contains("мыт") || t.Contains("голов") || t.Contains("шампун") => "Не мыть голову",
            _ => "Соблюдать ограничение"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException("RestrictionShortPhraseConverter does not support ConvertBack");
    }
} 