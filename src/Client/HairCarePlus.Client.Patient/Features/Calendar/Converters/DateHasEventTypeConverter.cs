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
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is DateTime date && parameter is EventType eventType)
                {
                    // Получаем текущую страницу
                    var currentPage = Application.Current?.MainPage?.Handler?.MauiContext?.Services?.GetService(typeof(TodayPage)) as TodayPage;
                    
                    // Получаем ViewModel
                    if (currentPage?.BindingContext is TodayViewModel viewModel)
                    {
                        // Проверяем, есть ли события указанного типа
                        return viewModel.GetEventCount(date, eventType) > 0;
                    }
                    
                    // Если страница или ViewModel не найдены, проверяем в текущем контексте привязки
                    if (Application.Current?.MainPage?.BindingContext is TodayViewModel mainViewModel)
                    {
                        return mainViewModel.GetEventCount(date, eventType) > 0;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in DateHasEventTypeConverter: {ex.Message}");
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 