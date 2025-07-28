using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Common.Converters;

/// <summary>
/// Converts absolute file path (string) to ImageSource for binding.
/// Handles iOS path quirks where "file://" prefix may be required.
/// </summary>
public sealed class FilePathToImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string filePath || string.IsNullOrEmpty(filePath))
            return null;

        return ImageSource.FromFile(filePath);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 