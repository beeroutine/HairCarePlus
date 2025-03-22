using System.Globalization;
using HairCarePlus.Client.Patient.Features.Calendar.Models;

namespace HairCarePlus.Client.Patient.Features.Calendar.Helpers
{
    public class EventTypeToLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EventType eventType)
            {
                return eventType switch
                {
                    EventType.Medication => "MEDICATION",
                    EventType.Photo => "PHOTO",
                    EventType.Restriction => "RESTRICTION",
                    EventType.Instruction => "INSTRUCTION",
                    _ => "EVENT"
                };
            }
            
            return "EVENT";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 