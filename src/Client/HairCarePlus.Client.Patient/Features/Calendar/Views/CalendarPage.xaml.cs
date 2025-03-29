using System;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using System.Linq;
using System.Text;
using System.Collections.Generic;

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
        private SimpleCalendarViewModel? ViewModel => BindingContext as SimpleCalendarViewModel;

        public CalendarPage()
        {
            try
            {
                InitializeComponent();
                
                // Создаем простую ViewModel без зависимостей
                BindingContext = new SimpleCalendarViewModel();
                System.Diagnostics.Debug.WriteLine("CalendarPage успешно инициализирована");
                
                // Подписываемся на событие ошибки в monthCalendarView
                monthCalendarView.ErrorOccurred += MonthCalendarView_ErrorOccurred;
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
                    // Выводим полную информацию об ошибке для отладки
                    string errorDetails = $"Сообщение: {ex.Message}\n";
                    errorDetails += $"Тип: {ex.GetType().Name}\n\n";
                    
                    if (ex.InnerException != null)
                    {
                        errorDetails += $"Внутреннее исключение: {ex.InnerException.Message}\n";
                        errorDetails += $"Тип: {ex.InnerException.GetType().Name}\n\n";
                    }
                    
                    errorDetails += $"Stack Trace:\n{ex.StackTrace}";
                    
                    await DisplayAlert("Подробности ошибки календаря", errorDetails, "OK");
                });
            }
        }

        // Обработчик ошибок из MonthCalendarView
        private void MonthCalendarView_ErrorOccurred(object sender, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Перехвачена ошибка из MonthCalendarView: {ex.Message}");
            
            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(async () => {
                // Выводим полную информацию об ошибке для отладки
                string errorDetails = $"Источник: MonthCalendarView\n";
                errorDetails += $"Сообщение: {ex.Message}\n";
                errorDetails += $"Тип: {ex.GetType().Name}\n\n";
                
                if (ex.InnerException != null)
                {
                    errorDetails += $"Внутреннее исключение: {ex.InnerException.Message}\n";
                    errorDetails += $"Тип: {ex.InnerException.GetType().Name}\n\n";
                }
                
                errorDetails += $"Stack Trace:\n{ex.StackTrace}";
                
                await DisplayAlert("Ошибка календаря", errorDetails, "OK");
            });
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            try
            {
                System.Diagnostics.Debug.WriteLine("CalendarPage.OnAppearing: начало выполнения");
                
                // Проверяем, что monthCalendarView инициализирован
                if (monthCalendarView == null)
                {
                    System.Diagnostics.Debug.WriteLine("CalendarPage.OnAppearing: monthCalendarView равен null");
                    return;
                }
                
                // Проверяем, что ViewModel инициализирована
                if (ViewModel == null)
                {
                    System.Diagnostics.Debug.WriteLine("CalendarPage.OnAppearing: ViewModel равен null, пересоздаем");
                    BindingContext = new SimpleCalendarViewModel();
                }
                
                // Добавляем кнопку диагностики, если ее нет
                if (!ToolbarItems.Any(i => i.Text == "Диагностика"))
                {
                    var diagnosticButton = new ToolbarItem
                    {
                        Text = "Диагностика",
                        Order = ToolbarItemOrder.Primary,
                        Priority = 0
                    };
                    
                    diagnosticButton.Clicked += DiagnosticButton_Clicked;
                    ToolbarItems.Add(diagnosticButton);
                    
                    System.Diagnostics.Debug.WriteLine("CalendarPage.OnAppearing: добавлена кнопка диагностики");
                }
                
                // Принудительно обновляем календарь при появлении страницы
                System.Diagnostics.Debug.WriteLine("CalendarPage.OnAppearing: вызываем RefreshCalendarDays");
                monthCalendarView.RefreshCalendarDays();
                System.Diagnostics.Debug.WriteLine("CalendarPage.OnAppearing: RefreshCalendarDays успешно выполнен");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в CalendarPage.OnAppearing: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                
                // Показываем пользователю сообщение об ошибке
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(async () => {
                    // Выводим полную информацию об ошибке для отладки
                    string errorDetails = $"Сообщение: {ex.Message}\n";
                    errorDetails += $"Тип: {ex.GetType().Name}\n\n";
                    
                    if (ex.InnerException != null)
                    {
                        errorDetails += $"Внутреннее исключение: {ex.InnerException.Message}\n";
                        errorDetails += $"Тип: {ex.InnerException.GetType().Name}\n\n";
                    }
                    
                    errorDetails += $"Stack Trace:\n{ex.StackTrace}";
                    
                    await DisplayAlert("Подробности ошибки календаря", errorDetails, "OK");
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
            sb.AppendLine($"ViewModel null: {ViewModel == null}");
            sb.AppendLine($"monthCalendarView null: {monthCalendarView == null}");
            
            if (ViewModel != null)
            {
                sb.AppendLine();
                sb.AppendLine("--- SimpleCalendarViewModel ---");
                sb.AppendLine($"CurrentMonthDate: {ViewModel.CurrentMonthDate:yyyy-MM-dd}");
                sb.AppendLine($"SelectedDate: {ViewModel.SelectedDate:yyyy-MM-dd}");
                sb.AppendLine($"IsMonthViewVisible: {ViewModel.IsMonthViewVisible}");
                sb.AppendLine($"HasSelectedDayEvents: {ViewModel.HasSelectedDayEvents}");
                
                if (ViewModel.SelectedDayEvents != null)
                {
                    sb.AppendLine($"SelectedDayEvents Count: {ViewModel.SelectedDayEvents.Count}");
                    foreach (var evt in ViewModel.SelectedDayEvents)
                    {
                        sb.AppendLine($" - {evt.Title} ({evt.EventType})");
                    }
                }
                else
                {
                    sb.AppendLine("SelectedDayEvents: null");
                }
            }
            
            // Пытаемся получить дополнительную информацию о состоянии дней календаря
            try
            {
                // Получаем все Frame элементы из MonthCalendarView
                var frames = monthCalendarView.FindVisualChildren<Frame>()
                    .Where(f => f.BindingContext is DateTime)
                    .ToList();
                
                if (frames != null && frames.Any())
                {
                    sb.AppendLine();
                    sb.AppendLine("--- Calendar Grid ---");
                    sb.AppendLine($"Cells Count: {frames.Count}");
                    
                    // Выводим информацию о текущем дне
                    var todayCell = frames.FirstOrDefault(f => 
                        f.BindingContext is DateTime dt && dt.Date == DateTime.Today);
                    
                    if (todayCell != null)
                    {
                        sb.AppendLine("Today's cell found in calendar grid");
                    }
                    else
                    {
                        sb.AppendLine("Today's cell NOT found in calendar grid");
                    }
                }
                else
                {
                    sb.AppendLine();
                    sb.AppendLine("--- Calendar Grid ---");
                    sb.AppendLine("No calendar cells found");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine();
                sb.AppendLine("--- Calendar Grid ---");
                sb.AppendLine($"Error getting calendar grid info: {ex.Message}");
            }
            
            return sb.ToString();
        }
    }
} 