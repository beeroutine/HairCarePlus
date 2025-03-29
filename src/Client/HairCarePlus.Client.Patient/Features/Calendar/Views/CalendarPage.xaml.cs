using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;

namespace HairCarePlus.Client.Patient.Features.Calendar.Views
{
    // Расширение для получения всех потомков визуального элемента
    public static class VisualTreeHelpers
    {
        public static IEnumerable<T> FindVisualChildren<T>(this Element element) where T : VisualElement
        {
            if (element == null)
                yield break;
            
            // Для ContentView получаем Content
            if (element is ContentView contentView && contentView.Content != null)
            {
                if (contentView.Content is T contentAsT)
                    yield return contentAsT;
                
                if (contentView.Content is Element contentAsElement)
                {
                    foreach (var child in contentAsElement.FindVisualChildren<T>())
                    {
                        yield return child;
                    }
                }
            }
            
            // Для Layout получаем все Children
            if (element is Layout layout)
            {
                foreach (var child in layout.Children)
                {
                    if (child is T childAsT)
                        yield return childAsT;
                    
                    if (child is Element childAsElement)
                    {
                        foreach (var grandChild in childAsElement.FindVisualChildren<T>())
                        {
                            yield return grandChild;
                        }
                    }
                }
            }
        }
    }

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CalendarPage : ContentPage
    {
        private SimpleCalendarViewModel _viewModel;
        
        public CalendarPage()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("CalendarPage: начало инициализации");
                InitializeComponent();
                
                // Инициализируем и устанавливаем ViewModel
                _viewModel = new SimpleCalendarViewModel();
                BindingContext = _viewModel;
                
                System.Diagnostics.Debug.WriteLine("CalendarPage: ViewModel успешно установлена");
                System.Diagnostics.Debug.WriteLine("CalendarPage: InitializeComponent выполнен успешно");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Критическая ошибка при инициализации CalendarPage: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(async () => {
                    await DisplayAlert("Ошибка", $"Произошла ошибка при загрузке календаря: {ex.Message}\n\nТип: {ex.GetType().Name}\n\nStack: {ex.StackTrace}", "OK");
                });
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            try
            {
                System.Diagnostics.Debug.WriteLine("CalendarPage.OnAppearing: начало выполнения");
                
                // Ничего особого не делаем, так как SimpleCalendarViewModel автоматически
                // загружает события для выбранного дня в конструкторе
                
                System.Diagnostics.Debug.WriteLine("CalendarPage.OnAppearing: успешно завершено");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в CalendarPage.OnAppearing: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(async () => {
                    await DisplayAlert("Ошибка", $"Произошла ошибка при обновлении календаря: {ex.Message}\n\nТип: {ex.GetType().Name}\n\nStack: {ex.StackTrace}", "OK");
                });
            }
        }
        
        // Обработчик нажатия на кнопку диагностики
        private async void DiagnosticButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                var diagnosticInfo = CollectDiagnosticInfo();
                await DisplayAlert("Диагностика календаря", diagnosticInfo, "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка диагностики", 
                    $"Не удалось собрать диагностическую информацию:\n{ex.Message}", "OK");
            }
        }
        
        // Метод для сбора диагностической информации
        private string CollectDiagnosticInfo()
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("=== ДИАГНОСТИКА КАЛЕНДАРЯ ===");
            sb.AppendLine($"Дата/время: {DateTime.Now}");
            sb.AppendLine();
            
            sb.AppendLine("--- CalendarPage ---");
            sb.AppendLine($"BindingContext null: {BindingContext == null}");
            sb.AppendLine($"BindingContext тип: {(BindingContext != null ? BindingContext.GetType().Name : "N/A")}");
            
            // Добавляем информацию о ViewModel
            if (BindingContext is SimpleCalendarViewModel vm)
            {
                sb.AppendLine();
                sb.AppendLine("--- SimpleCalendarViewModel ---");
                sb.AppendLine($"Текущий месяц: {vm.CurrentMonthYear}");
                sb.AppendLine($"Выбранная дата: {vm.SelectedDateText}");
                sb.AppendLine($"События для дня: {(vm.HasSelectedDayEvents ? vm.SelectedDayEvents.Count : 0)}");
                sb.AppendLine($"Режим месяца: {vm.IsMonthViewVisible}");
            }
            
            return sb.ToString();
        }
    }
} 