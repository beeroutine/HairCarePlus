using System.Globalization;

namespace HairCarePlus.Client.Patient.Features.Calendar.Converters
{
    public class EventTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string type)
            {
                return type switch
                {
                    "Medication" => "💊",
                    "Restriction" => "⛔",
                    "Instruction" => "ℹ️",
                    "Warning" => "⚠️",
                    "PhotoUpload" => "📷",
                    "Milestone" => "🏆",
                    "PRP" => "💉",
                    "WashingInstruction" => "💧",
                    "ProgressCheck" => "📊",
                    _ => "📅"
                };
            }
            return "📅";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 