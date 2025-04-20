using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;

namespace HairCarePlus.Client.Patient.Features.Calendar.Converters
{
    public class DateHasAnyEventColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is DateTime date && Application.Current != null)
                {
                    // Получаем ViewModel через BindingContext или другую привязку
                    TodayViewModel viewModel = null;
                    
                    // Безопасный доступ к MainPage и его BindingContext
                    if (Application.Current.MainPage != null && 
                        Application.Current.MainPage.BindingContext is TodayViewModel vm1)
                    {
                        viewModel = vm1;
                    }
                    // Пытаемся найти TodayViewModel в навигационном стеке, если он существует
                    else if (Application.Current.MainPage?.Navigation?.NavigationStack != null && 
                             Application.Current.MainPage.Navigation.NavigationStack.Count > 0 &&
                             Application.Current.MainPage.Navigation.NavigationStack[0]?.BindingContext is TodayViewModel vm2)
                    {
                        viewModel = vm2;
                    }

                    // Проверяем наличие событий только если viewModel и EventCountsByDate существуют
                    if (viewModel != null && viewModel.EventCountsByDate != null && 
                        viewModel.EventCountsByDate.TryGetValue(date.Date, out var eventCounts) && 
                        eventCounts != null)
                    {
                        // Если есть хотя бы одно событие любого типа
                        foreach (var count in eventCounts.Values)
                        {
                            if (count > 0)
                            {
                                // Определяем цвет для дней с событиями
                                // В темной теме - белый, в светлой - черный
                                return Application.Current.RequestedTheme == AppTheme.Dark
                                    ? Colors.White
                                    : Colors.Black;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // В случае любых исключений возвращаем цвет по умолчанию
                // Игнорируем ошибки, чтобы предотвратить крах приложения
            }
            
            // Если нет событий или произошла ошибка, возвращаем прозрачный цвет
            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 