using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using System.IO;

namespace HairCarePlus.Client.Clinic.Common.Converters;

public sealed class FilePathToImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path))
            return null;

        // If it's a local file path – load from disk.
        if (System.IO.File.Exists(path))
            return ImageSource.FromFile(path);

        // If it's a data URI (e.g., "data:image/jpeg;base64,...") decode inline.
        if (path.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var commaIdx = path.IndexOf(',');
                if (commaIdx >= 0)
                {
                    var base64 = path[(commaIdx + 1)..];
                    var bytes = System.Convert.FromBase64String(base64);
                    return ImageSource.FromStream(() => new MemoryStream(bytes));
                }
            }
            catch
            {
                // fall through to URI handling
            }
        }

        // Otherwise treat as absolute/relative URI.
        if (!path.StartsWith("file://", StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            path = $"file://{path}";
        }

        return ImageSource.FromUri(new Uri(path));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
} 