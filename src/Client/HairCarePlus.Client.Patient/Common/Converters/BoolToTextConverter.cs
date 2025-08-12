using System.Globalization;

namespace HairCarePlus.Client.Patient.Common.Converters
{
    public class BoolToTextConverter : IValueConverter
    {
        public string TrueText { get; set; } = string.Empty;
        public string FalseText { get; set; } = string.Empty;

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? TrueText : FalseText;
            return FalseText;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 