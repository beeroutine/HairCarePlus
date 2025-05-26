using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Progress.Converters;

/// <summary>
/// Конвертер для проверки равенства значения с параметром.
/// Используется для условной анимации критичных ограничений.
/// </summary>
public class EqualToConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        // Сравниваем значения, приводя их к строковому типу для универсальности
        var valueString = value.ToString();
        var parameterString = parameter.ToString();

        return string.Equals(valueString, parameterString, StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("EqualToConverter does not support ConvertBack");
    }
} 