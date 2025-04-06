using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;

namespace HairCarePlus.Client.Patient.Features.Calendar.Converters
{
    public class DateHasAnyEventConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is DateTime date)
                {
                    // Получаем ViewModel через BindingContext или другую привязку
                    TodayViewModel viewModel = null;
                    
                    if (Application.Current?.MainPage?.BindingContext is TodayViewModel vm1)
                    {
                        viewModel = vm1;
                    }
                    else if (Application.Current?.MainPage?.Navigation?.NavigationStack != null && 
                        Application.Current.MainPage.Navigation.NavigationStack.Count > 0 &&
                        Application.Current.MainPage.Navigation.NavigationStack[0]?.BindingContext is TodayViewModel vm2)
                    {
                        viewModel = vm2;
                    }

                    // Проверяем наличие событий в указанную дату
                    bool hasEvents = false;
                    if (viewModel?.EventCountsByDate != null && 
                        viewModel.EventCountsByDate.TryGetValue(date.Date, out var eventCounts))
                    {
                        // Если есть хотя бы одно событие любого типа, устанавливаем флаг
                        foreach (var count in eventCounts.Values)
                        {
                            if (count > 0)
                            {
                                hasEvents = true;
                                break;
                            }
                        }
                    }
                    
                    // Если параметр TextColor, возвращаем цвет текста в зависимости от наличия событий и темы
                    if (parameter is string paramText && paramText == "TextColor")
                    {
                        if (hasEvents)
                        {
                            // Для дней с событиями возвращаем белый (светлая тема) или черный (темная тема) текст
                            return Application.Current?.RequestedTheme == AppTheme.Dark
                                ? Colors.Black   // В темной теме черный текст на белом фоне
                                : Colors.White;  // В светлой теме белый текст на черном фоне
                        }
                        else
                        {
                            // Для дней без событий возвращаем серый текст
                            return Application.Current?.RequestedTheme == AppTheme.Dark
                                ? Color.FromArgb("#BDBDBD")  // Gray400 для темной темы
                                : Color.FromArgb("#9E9E9E");  // Gray600 для светлой темы
                        }
                    }
                    
                    // Если параметр не TextColor, возвращаем булево значение наличия событий
                    return hasEvents;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in DateHasAnyEventConverter: {ex.Message}");
            }
            
            if (parameter is string paramDefault && paramDefault == "TextColor")
            {
                return Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#BDBDBD")  // Gray400 для темной темы
                    : Color.FromArgb("#9E9E9E");  // Gray600 для светлой темы
            }
            
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 