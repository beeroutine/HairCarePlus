using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using System.Collections.Generic;
using System.Windows.Input;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;
using HairCarePlus.Client.Patient.Features.Calendar.Models;

namespace HairCarePlus.Client.Patient.Features.Calendar.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MonthCalendarView : ContentView
    {
        private readonly Frame[,] _dayCells = new Frame[6, 7];
        private readonly Label[,] _dayLabels = new Label[6, 7];
        private readonly BoxView[,] _eventIndicators = new BoxView[6, 7];
        private CalendarDayViewModel _selectedDay = null;
        
        private CalendarViewModel ViewModel => BindingContext as CalendarViewModel;

        public MonthCalendarView()
        {
            InitializeComponent();
            CreateCalendarCells();
            
            // Подписываемся на изменения BindingContext
            this.BindingContextChanged += OnBindingContextChanged;
        }
        
        private void OnBindingContextChanged(object sender, EventArgs e)
        {
            if (ViewModel != null)
            {
                // Подписываемся на изменения свойств ViewModel
                ViewModel.PropertyChanged += OnViewModelPropertyChanged;
                UpdateCalendarDays();
            }
        }
        
        private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CalendarViewModel.CalendarDays) || 
                e.PropertyName == nameof(CalendarViewModel.SelectedDate) ||
                e.PropertyName == nameof(CalendarViewModel.CurrentMonthDate))
            {
                UpdateCalendarDays();
            }
        }
        
        private void CreateCalendarCells()
        {
            // Создаем ячейки календаря программно
            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 7; col++)
                {
                    // Создаем индикатор событий
                    var eventIndicator = new BoxView
                    {
                        Style = (Style)Resources["EventDotStyle"],
                        IsVisible = false
                    };
                    _eventIndicators[row, col] = eventIndicator;
                    
                    // Создаем метку для числа
                    var dayLabel = new Label
                    {
                        Style = (Style)Resources["DayNumberStyle"],
                        Text = ""
                    };
                    _dayLabels[row, col] = dayLabel;
                    
                    // Создаем горизонтальный стек для индикаторов событий
                    var eventIndicatorsStack = new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        Spacing = 2,
                        HorizontalOptions = LayoutOptions.Center,
                        Children = { eventIndicator }
                    };
                    
                    // Создаем вертикальный стек для текста дня и индикаторов
                    var stack = new StackLayout
                    {
                        Spacing = 2,
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center,
                        Children = { dayLabel, eventIndicatorsStack }
                    };
                    
                    // Создаем ячейку дня
                    var dayCell = new Frame
                    {
                        Style = (Style)Resources["CalendarCellStyle"],
                        Content = stack,
                        GestureRecognizers = {
                            new TapGestureRecognizer { CommandParameter = new CalendarCellInfo(row, col) }
                        }
                    };
                    
                    // Добавляем обработчик тапа
                    ((TapGestureRecognizer)dayCell.GestureRecognizers[0]).Tapped += OnDayCellTapped;
                    
                    // Добавляем ячейку в Grid
                    CalendarGrid.Add(dayCell, col, row);
                    _dayCells[row, col] = dayCell;
                }
            }
        }
        
        private void OnDayCellTapped(object sender, EventArgs e)
        {
            if (sender is Element element && 
                element.BindingContext is CalendarDayViewModel dayViewModel && 
                ViewModel != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // Снимаем выделение с предыдущей ячейки
                    if (_selectedDay != null)
                    {
                        var oldRow = GetRowForDay(_selectedDay);
                        var oldCol = GetColumnForDay(_selectedDay);
                        if (oldRow >= 0 && oldCol >= 0)
                        {
                            UpdateCellAppearance(oldRow, oldCol, _selectedDay, false);
                        }
                    }
                    
                    // Выделяем новую ячейку
                    _selectedDay = dayViewModel;
                    var row = GetRowForDay(dayViewModel);
                    var col = GetColumnForDay(dayViewModel);
                    if (row >= 0 && col >= 0)
                    {
                        UpdateCellAppearance(row, col, dayViewModel, true);
                    }
                    
                    // Обновляем ViewModel
                    ViewModel.SelectedDate = dayViewModel.Date;
                    ViewModel.DaySelectedCommand?.Execute(dayViewModel.Date);
                });
            }
        }

        private void UpdateCalendarDays()
        {
            if (ViewModel?.CalendarDays == null || ViewModel.CalendarDays.Count < 42)
                return;
                
            // Подготовим все данные вне UI-потока
            var updatesRequired = new List<(int Row, int Col, CalendarDayViewModel Day, bool IsSelected)>();
            CalendarDayViewModel selectedDay = null;
            
            // Предварительное вычисление и сбор обновлений
            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 7; col++)
                {
                    int index = row * 7 + col;
                    if (index < ViewModel.CalendarDays.Count)
                    {
                        var dayViewModel = ViewModel.CalendarDays[index];
                        bool isSelected = dayViewModel.IsSelected;
                        
                        // Добавляем это обновление в список
                        updatesRequired.Add((row, col, dayViewModel, isSelected));
                        
                        // Если это выбранный день, сохраняем ссылку
                        if (isSelected)
                        {
                            selectedDay = dayViewModel;
                        }
                    }
                }
            }
            
            // Применяем обновления на UI-потоке одним вызовом
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    _selectedDay = selectedDay;
                    
                    // Применяем все подготовленные обновления
                    foreach (var update in updatesRequired)
                    {
                        // Сохраняем ссылку на ViewModel в ячейке
                        _dayCells[update.Row, update.Col].BindingContext = update.Day;
                        
                        // Обновляем внешний вид ячейки
                        UpdateCellAppearance(update.Row, update.Col, update.Day, update.IsSelected);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating calendar: {ex.Message}");
                }
            });
        }
        
        private void UpdateCellAppearance(int row, int col, CalendarDayViewModel dayViewModel, bool isSelected)
        {
            if (row < 0 || row >= 6 || col < 0 || col >= 7 || dayViewModel == null)
                return;
                
            try
            {
                // Обновляем текст
                _dayLabels[row, col].Text = dayViewModel.Date.Day.ToString();
                
                // Обновляем цвет фона
                Color selectedColor = Colors.Transparent;
                if (Application.Current.Resources.TryGetValue("SelectedDayColor", out var color) && color is Color)
                {
                    selectedColor = (Color)color;
                }
                else
                {
                    selectedColor = Color.FromArgb("#6962AD"); // Запасной цвет
                }
                
                _dayCells[row, col].BackgroundColor = isSelected ? selectedColor : Colors.Transparent;
                
                // Обновляем цвет текста
                Color textColor = Colors.Black;
                Color disabledTextColor = Colors.Gray;
                
                if (Application.Current.Resources.TryGetValue("TextPrimaryColor", out var tColor) && tColor is Color)
                {
                    textColor = (Color)tColor;
                }
                
                if (Application.Current.Resources.TryGetValue("DisabledTextColor", out var dtColor) && dtColor is Color)
                {
                    disabledTextColor = (Color)dtColor;
                }
                
                _dayLabels[row, col].TextColor = isSelected 
                    ? Colors.White 
                    : dayViewModel.IsCurrentMonth ? textColor : disabledTextColor;
                    
                // Обновляем прозрачность ячейки
                _dayCells[row, col].Opacity = dayViewModel.IsCurrentMonth ? 1.0 : 0.3;
                
                // Обновляем индикаторы событий только если день в текущем месяце и имеет события
                if (dayViewModel.HasEvents && dayViewModel.IsCurrentMonth)
                {
                    UpdateEventIndicators(row, col, dayViewModel);
                }
                else
                {
                    // Очищаем индикаторы, если нет событий
                    var stack = (_dayCells[row, col].Content as StackLayout)?.Children[1] as StackLayout;
                    if (stack != null)
                    {
                        stack.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating cell appearance: {ex.Message}");
            }
        }
        
        private void UpdateEventIndicators(int row, int col, CalendarDayViewModel dayViewModel)
        {
            // Получаем стек индикаторов
            var stack = (_dayCells[row, col].Content as StackLayout)?.Children[1] as StackLayout;
            if (stack == null) return;
            
            // Очищаем существующие индикаторы
            stack.Clear();
            
            // Если нет событий или день не в текущем месяце, выходим
            if (!dayViewModel.HasEvents || !dayViewModel.IsCurrentMonth || ViewModel?.EventsForMonth == null)
                return;
            
            var date = dayViewModel.Date.Date;
            var events = ViewModel.EventsForMonth.Where(e => e.Date.Date == date).ToList();
            
            if (events.Count == 0)
                return;
            
            // Создаем один индикатор для всех событий
            var indicator = new BoxView
            {
                Style = (Style)Resources["EventDotStyle"]
            };
            
            // Приоритет цветов: Restriction > Medication > Photo > Instruction
            EventType priorityType = GetPriorityEventType(events);
            
            // Устанавливаем цвет индикатора
            try 
            {
                string resourceKey = priorityType switch
                {
                    EventType.Restriction => "RestrictionColor",
                    EventType.Medication => "MedicationColor",
                    EventType.Photo => "PhotoColor", 
                    EventType.Instruction => "InstructionColor",
                    _ => "EventIndicatorColor"
                };
                
                if (Application.Current.Resources.TryGetValue(resourceKey, out var color))
                {
                    indicator.BackgroundColor = (Color)color;
                }
                else
                {
                    indicator.BackgroundColor = (Color)Application.Current.Resources["EventIndicatorColor"];
                }
            }
            catch
            {
                // В случае ошибки используем стандартный цвет индикатора
                indicator.BackgroundColor = (Color)Application.Current.Resources["EventIndicatorColor"];
            }
            
            stack.Add(indicator);
        }
        
        private EventType GetPriorityEventType(List<CalendarEvent> events)
        {
            // Приоритет: Restriction > Medication > Photo > Instruction
            var eventTypes = events.Select(e => e.EventType).Distinct().ToList();
            
            if (eventTypes.Contains(EventType.Restriction))
                return EventType.Restriction;
            if (eventTypes.Contains(EventType.Medication))
                return EventType.Medication;
            if (eventTypes.Contains(EventType.Photo))
                return EventType.Photo;
            if (eventTypes.Contains(EventType.Instruction))
                return EventType.Instruction;
                
            return EventType.Instruction; // Default fallback
        }
        
        private int GetRowForDay(CalendarDayViewModel day)
        {
            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 7; col++)
                {
                    if (_dayCells[row, col].BindingContext == day)
                    {
                        return row;
                    }
                }
            }
            return -1;
        }
        
        private int GetColumnForDay(CalendarDayViewModel day)
        {
            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 7; col++)
                {
                    if (_dayCells[row, col].BindingContext == day)
                    {
                        return col;
                    }
                }
            }
            return -1;
        }
    }
    
    public class CalendarCellInfo
    {
        public int Row { get; }
        public int Column { get; }
        
        public CalendarCellInfo(int row, int column)
        {
            Row = row;
            Column = column;
        }
    }
    
    public class CalendarDayViewModel : BindableObject
    {
        private DateTime _date;
        private bool _isCurrentMonth;
        private bool _hasEvents;
        private bool _isSelected;

        public DateTime Date
        {
            get => _date;
            set
            {
                if (_date != value)
                {
                    _date = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsCurrentMonth
        {
            get => _isCurrentMonth;
            set
            {
                if (_isCurrentMonth != value)
                {
                    _isCurrentMonth = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasEvents
        {
            get => _hasEvents;
            set
            {
                if (_hasEvents != value)
                {
                    _hasEvents = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }
    }
} 