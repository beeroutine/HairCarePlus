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
        public CalendarPage()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("CalendarPage: начало инициализации");
                InitializeComponent();
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
                    await DisplayAlert("Ошибка", $"Произошла ошибка при загрузке календаря: {ex.Message}", "OK");
                });
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            try
            {
                System.Diagnostics.Debug.WriteLine("CalendarPage.OnAppearing: начало выполнения");                
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
            
            return sb.ToString();
        }
    }
} 