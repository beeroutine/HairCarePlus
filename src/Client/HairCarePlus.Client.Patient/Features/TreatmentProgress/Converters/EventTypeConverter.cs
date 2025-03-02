using System.Globalization;
using HairCarePlus.Client.Patient.Features.TreatmentProgress.Models;

namespace HairCarePlus.Client.Patient.Features.TreatmentProgress.Converters
{
    public class EventTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string typeString && Enum.TryParse<EventType>(typeString, out var type))
            {
                return type switch
                {
                    EventType.Medication => "💊",
                    EventType.PhotoReport => "📷",
                    EventType.Restriction => "⛔",
                    EventType.Care => "🧴",
                    EventType.Recommendation => "ℹ️",
                    EventType.PlasmaTherapy => "💉",
                    EventType.Vitamins => "🍊",
                    EventType.Milestone => "🏆",
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
    
    public class EventTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EventType type)
            {
                return type switch
                {
                    EventType.Medication => Colors.Blue,
                    EventType.PhotoReport => Colors.Purple,
                    EventType.Restriction => Colors.Red,
                    EventType.Care => Colors.Teal,
                    EventType.Recommendation => Colors.Green,
                    EventType.PlasmaTherapy => Colors.Orange,
                    EventType.Vitamins => Colors.Orange,
                    EventType.Milestone => Colors.Purple,
                    _ => Colors.Gray
                };
            }
            
            // Try parsing the string value
            if (value is string typeString && Enum.TryParse<EventType>(typeString, out var parsedType))
            {
                return parsedType switch
                {
                    EventType.Medication => Colors.Blue,
                    EventType.PhotoReport => Colors.Purple,
                    EventType.Restriction => Colors.Red,
                    EventType.Care => Colors.Teal,
                    EventType.Recommendation => Colors.Green,
                    EventType.PlasmaTherapy => Colors.Orange,
                    EventType.Vitamins => Colors.Orange,
                    EventType.Milestone => Colors.Purple,
                    _ => Colors.Gray
                };
            }
            
            return Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 