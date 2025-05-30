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
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path))
            return null;

        // Prefer FromFile for local absolute paths – works across iOS/Android without URI scheme.
        if (System.IO.File.Exists(path))
            return ImageSource.FromFile(path);

        // Fallback: try URI (useful if path already contains scheme)
        if (!path.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            path = $"file://{path}";

        return ImageSource.FromUri(new Uri(path));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
} 