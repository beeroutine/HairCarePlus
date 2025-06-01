using System;
using System.Globalization;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;
using HairCarePlus.Client.Patient.Features.Calendar.Views;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Calendar.Converters
{
    public class DateHasEventTypeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            try
            {
                if (value is DateTime date && parameter is EventType eventType)
                {
                    // Получаем текущую страницу из Shell без создания новой
                    var currentPage = Shell.Current?.CurrentPage as TodayPage;
                    
                    if (currentPage?.BindingContext is TodayViewModel viewModel)
                    {
                        // Проверяем, есть ли события указанного типа в ViewModel
                        return viewModel.GetEventCount(date, eventType) > 0;
                    }
                    
                    // Fallback: попробовать получить ViewModel из текущей страницы, если это не TodayPage
                    if (Shell.Current?.CurrentPage?.BindingContext is TodayViewModel fallbackVm)
                    {
                        return fallbackVm.GetEventCount(date, eventType) > 0;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Error in DateHasEventTypeConverter: {ex.Message}");
#endif
                return false;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 