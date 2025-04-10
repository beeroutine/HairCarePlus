using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using HairCarePlus.Client.Patient.Features.Calendar.Models;

namespace HairCarePlus.Client.Patient.Features.Calendar.Converters
{
    public class EventTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EventType eventType)
            {
                return eventType switch
                {
                    EventType.MedicationTreatment => Application.Current.Resources["MedicationBlue"],
                    EventType.MedicalVisit => Application.Current.Resources["AppointmentGreen"],
                    EventType.Photo => Application.Current.Resources["PhotoPurple"],
                    EventType.Video => Application.Current.Resources["VideoOrange"],
                    EventType.GeneralRecommendation => Application.Current.Resources["GeneralGray"],
                    EventType.CriticalWarning => Colors.Red,
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