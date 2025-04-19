using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using HairCarePlus.Client.Patient.Common.Behaviors;

namespace HairCarePlus.Client.Patient.Features.Chat.Converters;

public class MessageBackgroundConverter : IValueConverter
{
    private static readonly ILogger<MessageBackgroundConverter> _logger =
        ServiceHelper.GetService<ILogger<MessageBackgroundConverter>>() ?? NullLogger<MessageBackgroundConverter>.Instance;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string senderId)
        {
            _logger.LogDebug("Convert: Value is null or not a string");
            return null;
        }

        _logger.LogDebug("Convert invoked. senderId={SenderId}, targetType={TargetType}, parameter={Parameter}", senderId, targetType.Name, parameter);
        
        // Determine current theme safely
        var currentApp = Application.Current;
        var currentTheme = currentApp?.RequestedTheme ?? AppTheme.Light; // Default to Light if null
        
        // Define colors based on theme
        Color patientBgColor = currentTheme == AppTheme.Dark ? Color.FromArgb("#1E2A35") : Color.FromArgb("#EAF4FC");
        Color doctorBgColor = currentTheme == AppTheme.Dark ? Color.FromArgb("#4D7B63") : Color.FromArgb("#A0DAB2");
        Color defaultBgColor = currentTheme == AppTheme.Dark ? Color.FromArgb("#222222") : Color.FromArgb("#F7F7F7");

        // Check if the target is Color or the parameter specifies background color
        bool wantsBackgroundColor = targetType == typeof(Color) || 
                                    (parameter is string pStr && pStr.Equals("backgroundcolor", StringComparison.OrdinalIgnoreCase));

        if (wantsBackgroundColor)
        {
            if (senderId == "patient") return patientBgColor;
            if (senderId == "doctor") return doctorBgColor;
            return defaultBgColor; // fallback
        }

        if (parameter is string conversionType)
        {
            switch (conversionType.ToLowerInvariant())
            {
                case "column":
                    return senderId == "patient" ? 1 : 0;
                case "alignment":
                    return senderId == "patient" ? LayoutOptions.End : LayoutOptions.Start;
                case "margin":
                    // Consider making Thickness calculation more robust or theme-dependent if needed
                    return senderId == "patient" ? new Thickness(80, 0, 8, 0) : new Thickness(8, 0, 80, 0);
                default:
                    _logger.LogDebug("Convert: Unknown parameter type: {Parameter}", conversionType);
                    return null; // Return null for unknown parameters
            }
        }
        _logger.LogDebug("Convert: Parameter is null or not a string");
        return null; // Return null if parameter is not a valid string or processing fails
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 