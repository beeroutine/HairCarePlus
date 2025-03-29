using System;
using System.Globalization;
using HairCarePlus.Client.Patient.Features.DailyRoutine.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace HairCarePlus.Client.Patient.Features.DailyRoutine.Converters
{
    public class TaskTypeToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is TaskType taskType)
            {
                return taskType switch
                {
                    TaskType.Medication => Application.Current?.Resources["MedicationBlue"] ?? Colors.Blue,
                    TaskType.Appointment => Application.Current?.Resources["AppointmentGreen"] ?? Colors.Green,
                    TaskType.Photo => Application.Current?.Resources["PhotoPurple"] ?? Colors.Purple,
                    TaskType.Video => Application.Current?.Resources["VideoOrange"] ?? Colors.Orange,
                    TaskType.General => Application.Current?.Resources["GeneralGray"] ?? Colors.Gray,
                    _ => Colors.Gray
                };
            }
            return Colors.Gray;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TaskTypeToIconConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is TaskType taskType)
            {
                return taskType switch
                {
                    TaskType.Medication => Application.Current?.Resources["IconMedication"],
                    TaskType.Appointment => Application.Current?.Resources["IconMedicalServices"],
                    TaskType.Photo => Application.Current?.Resources["IconCamera"],
                    TaskType.Video => Application.Current?.Resources["IconPlayCircle"],
                    TaskType.General => Application.Current?.Resources["IconInfo"],
                    _ => Application.Current?.Resources["IconInfo"]
                };
            }
            return Application.Current?.Resources["IconInfo"];
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 