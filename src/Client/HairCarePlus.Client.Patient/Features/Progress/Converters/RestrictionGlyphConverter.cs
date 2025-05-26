using System;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Progress.Converters;

/// <summary>
/// Возвращает глиф FontAwesome в зависимости от названия ограничения.
/// Fallback – иконка запрета (🚫).
/// </summary>
public sealed class RestrictionGlyphConverter : IValueConverter
{
    private static readonly Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase)
    {
        // Russian substrings
        {"басс", "\uf2dc"},     // swimmer
        {"плаван", "\uf2dc"},   // swimmer
        {"вода", "\uf2dc"},     // water -> swimmer
        {"душ", "\uf2dc"},      // shower -> swimmer
        {"солярий", "\uf2dc"},  // solarium -> treat as swimming restriction
        {"стриж", "\uf0c4"},     // scissors
        {"загар", "\uf185"},     // sun
        {"спорт", "\uf44b"},     // dumbbell
        {"алког", "\uf0fc"},     // martini-glass
        {"сауна", "\uf2dc"},     // hot-tub

        // English keywords for backend data consistency
        {"swim", "\uf2dc"},
        {"pool", "\uf2dc"},
        {"haircut", "\uf0c4"},
        {"sun", "\uf185"},
        {"sport", "\uf44b"},
        {"gym", "\uf44b"},
        {"alcohol", "\uf0fc"},
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string title)
            return "🚫";

        foreach (var kvp in _map)
        {
            if (title.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }

        return "🚫";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
} 