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
                
                // Инициализируем и устанавливаем ViewModel перед InitializeComponent
                // для предотвращения проблем с привязкой данных
                _viewModel = new SimpleCalendarViewModel();
                
                // Теперь инициализируем компоненты
                InitializeComponent();
                
                // Устанавливаем контекст привязки
                BindingContext = _viewModel;
                
                // Подписываемся на изменение месяца для обновления сетки
                _viewModel.CurrentMonthChanged += OnCurrentMonthChanged;
                
                // Генерируем сетку календаря для текущего месяца
                GenerateCalendarGrid(_viewModel.CurrentMonthDate);
                
                System.Diagnostics.Debug.WriteLine("CalendarPage: ViewModel успешно установлена");
                System.Diagnostics.Debug.WriteLine("CalendarPage: InitializeComponent выполнен успешно");
            }
            catch (InvalidCastException icEx)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка преобразования типов при инициализации CalendarPage: {icEx.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {icEx.StackTrace}");
                
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(async () => {
                    await DisplayAlert("Ошибка", $"Произошла ошибка при загрузке календаря: {icEx.Message}\n\nТип: InvalidCastException\n\nЭта ошибка связана с несоответствием типов данных в XAML.", "OK");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Критическая ошибка при инициализации CalendarPage: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"Inner exception type: {ex.InnerException.GetType().Name}");
                }
                
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(async () => {
                    await DisplayAlert("Ошибка", $"Произошла ошибка при загрузке календаря: {ex.Message}\n\nТип: {ex.GetType().Name}\n\nStack: {ex.StackTrace}", "OK");
                });
            }
        }

        protected override void OnAppearing()
        {
            try
            {
                // Сначала вызываем базовый метод
                base.OnAppearing();
                
                System.Diagnostics.Debug.WriteLine("CalendarPage.OnAppearing: начало выполнения");
                
                // Проверяем, что ViewModel инициализирована
                if (_viewModel == null)
                {
                    System.Diagnostics.Debug.WriteLine("CalendarPage.OnAppearing: _viewModel не инициализирована, выполняем инициализацию");
                    _viewModel = new SimpleCalendarViewModel();
                    BindingContext = _viewModel;
                    
                    // Подписываемся на изменение месяца
                    _viewModel.CurrentMonthChanged += OnCurrentMonthChanged;
                    
                    // Генерируем сетку календаря
                    GenerateCalendarGrid(_viewModel.CurrentMonthDate);
                }
                
                System.Diagnostics.Debug.WriteLine("CalendarPage.OnAppearing: успешно завершено");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в CalendarPage.OnAppearing: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"Inner exception type: {ex.InnerException.GetType().Name}");
                }
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(async () => {
                    await DisplayAlert("Ошибка", $"Произошла ошибка при обновлении календаря: {ex.Message}\n\nТип: {ex.GetType().Name}", "OK");
                });
            }
        }
        
        // Обработчик изменения месяца в ViewModel
        private void OnCurrentMonthChanged(object sender, EventArgs e)
        {
            try
            {
                if (_viewModel != null)
                {
                    // Обновляем сетку календаря при изменении месяца
                    GenerateCalendarGrid(_viewModel.CurrentMonthDate);
                    System.Diagnostics.Debug.WriteLine($"Обновлена сетка для месяца: {_viewModel.CurrentMonthYear}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при обновлении сетки календаря: {ex.Message}");
            }
        }
        
        // Метод для динамической генерации сетки календаря
        private void GenerateCalendarGrid(DateTime month)
        {
            try
            {
                // Очищаем текущую сетку
                CalendarGrid.Children.Clear();
                
                // Получаем первый день месяца
                var firstDayOfMonth = new DateTime(month.Year, month.Month, 1);
                
                // Определяем, на какой день недели приходится первый день месяца
                // (0 = воскресенье, 1 = понедельник, ..., 6 = суббота)
                int dayOfWeek = (int)firstDayOfMonth.DayOfWeek;
                // Преобразуем, чтобы понедельник был 0, воскресенье было 6
                dayOfWeek = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
                
                // Получаем количество дней в месяце
                int daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);
                
                // Текущая дата для определения сегодняшнего дня
                var today = DateTime.Today;
                bool isCurrentMonth = (month.Year == today.Year && month.Month == today.Month);
                
                // Заполняем пустые ячейки до первого дня месяца
                for (int i = 0; i < dayOfWeek; i++)
                {
                    var emptyLabel = new Label
                    {
                        Text = "",
                        Style = (Style)Resources["CalendarDayLabelStyle"]
                    };
                    CalendarGrid.Add(emptyLabel, i, 0);
                }
                
                // Заполняем дни месяца
                int row = 0;
                int col = dayOfWeek;
                
                for (int day = 1; day <= daysInMonth; day++)
                {
                    var dayButton = new Button
                    {
                        Text = day.ToString(),
                        CommandParameter = day.ToString()
                    };
                    
                    // Устанавливаем команду
                    dayButton.Command = _viewModel.DaySelectedCommand;
                    
                    // Применяем стиль в зависимости от того, текущий ли это день
                    if (isCurrentMonth && day == today.Day)
                    {
                        dayButton.Style = (Style)Resources["CurrentDayButtonStyle"];
                    }
                    else
                    {
                        dayButton.Style = (Style)Resources["CalendarDayButtonStyle"];
                    }
                    
                    // Добавляем кнопку в сетку
                    CalendarGrid.Add(dayButton, col, row);
                    
                    // Переходим к следующей ячейке
                    col++;
                    if (col > 6)
                    {
                        col = 0;
                        row++;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"Сгенерирована сетка для месяца {month:MMMM yyyy}, дней: {daysInMonth}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при генерации сетки календаря: {ex.Message}");
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
            sb.AppendLine($"_viewModel null: {_viewModel == null}");
            
            // Добавляем информацию о ViewModel
            if (BindingContext is SimpleCalendarViewModel vm)
            {
                sb.AppendLine();
                sb.AppendLine("--- SimpleCalendarViewModel ---");
                sb.AppendLine($"Текущий месяц: {vm.CurrentMonthYear}");
                sb.AppendLine($"Выбранная дата: {vm.SelectedDateText}");
                sb.AppendLine($"События для дня: {(vm.HasSelectedDayEvents ? vm.SelectedDayEvents?.Count : 0)}");
                sb.AppendLine($"Режим месяца: {vm.IsMonthViewVisible}");
            }
            
            return sb.ToString();
        }
    }
} 