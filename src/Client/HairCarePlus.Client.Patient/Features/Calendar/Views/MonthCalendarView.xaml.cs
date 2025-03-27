using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using System.Collections.ObjectModel;
using System.Windows.Input;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using System.Threading.Tasks;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui;

namespace HairCarePlus.Client.Patient.Features.Calendar.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MonthCalendarView : ContentView
    {
        private readonly Dictionary<DateTime, Frame> _dateCells = new();
        private SimpleCalendarViewModel? _previousViewModel;
        private SimpleCalendarViewModel? ViewModel => BindingContext as SimpleCalendarViewModel;

        public MonthCalendarView()
        {
            InitializeComponent();
            CreateCalendarCells();
            
            // Подписываемся на изменение BindingContext
            this.BindingContextChanged += OnBindingContextChanged;
            
            // Подписываемся на изменения размера для обновления ячеек
            this.SizeChanged += (s, e) => RefreshCalendarDays();
        }
        
        private void OnBindingContextChanged(object sender, EventArgs e)
        {
            // Отписываемся от событий старой ViewModel
            if (_previousViewModel != null)
            {
                _previousViewModel.CurrentMonthChanged -= ViewModel_CurrentMonthChanged;
                _previousViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
            
            // Подписываемся на события новой ViewModel
            if (ViewModel != null)
            {
                ViewModel.CurrentMonthChanged += ViewModel_CurrentMonthChanged;
                ViewModel.PropertyChanged += ViewModel_PropertyChanged;
                
                // Обновляем календарь
                RefreshCalendarDays();
                
                // Запоминаем текущую ViewModel
                _previousViewModel = ViewModel;
            }
        }
        
        private void ViewModel_CurrentMonthChanged(object sender, EventArgs e)
        {
            // Обновляем календарь при смене месяца
            RefreshCalendarDays();
        }
        
        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Обновляем календарь при изменении даты
            if (e.PropertyName == nameof(SimpleCalendarViewModel.CurrentMonthDate) || 
                e.PropertyName == nameof(SimpleCalendarViewModel.SelectedDate))
            {
                RefreshCalendarDays();
            }
        }
        
        private void CreateCalendarCells()
        {
            // Очищаем существующие ячейки
            CalendarGrid.Children.Clear();
            
            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 7; col++)
                {
                    var cell = CreateDayCell();
                    Grid.SetRow(cell, row);
                    Grid.SetColumn(cell, col);
                    CalendarGrid.Children.Add(cell);
                }
            }
        }

        private Frame CreateDayCell()
        {
            var cellGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Star }
                },
                Padding = new Thickness(0),
                BackgroundColor = Colors.Transparent
            };

            var dayLabel = new Label
            {
                Style = (Style)Resources["DayNumberStyle"]
            };

            cellGrid.Children.Add(dayLabel);

            var frame = new Frame
            {
                Style = (Style)Resources["CalendarCellStyle"],
                Content = cellGrid
            };

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += OnDayCellTapped;
            frame.GestureRecognizers.Add(tapGesture);

            return frame;
        }

        private void OnDayCellTapped(object sender, EventArgs e)
        {
            if (sender is Frame frame && frame.BindingContext is DateTime selectedDate && ViewModel != null)
            {
                if (ViewModel.DaySelectedCommand?.CanExecute(selectedDate) == true)
                {
                    ViewModel.DaySelectedCommand.Execute(selectedDate);
                }
            }
        }

        public void RefreshCalendarDays()
        {
            try
            {
                // Получаем месяц для отображения
                DateTime currentMonth = ViewModel?.CurrentMonthDate ?? DateTime.Today;
                
                // Рассчитываем первый день в календаре (может быть из предыдущего месяца)
                var firstDayOfMonth = new DateTime(currentMonth.Year, currentMonth.Month, 1);
                var firstDayOfCalendar = firstDayOfMonth.AddDays(-(int)firstDayOfMonth.DayOfWeek);
                
                // Обрабатываем все ячейки календаря
                var cells = CalendarGrid.Children.OfType<Frame>().ToList();
                var currentDate = firstDayOfCalendar;
                
                foreach (var cell in cells)
                {
                    // Очищаем содержимое ячейки
                    var grid = cell.Content as Grid;
                    if (grid == null) continue;
                    
                    grid.Children.Clear();
                    
                    // Определяем, является ли ячейка текущим днем
                    bool isToday = currentDate.Date == DateTime.Today;
                    bool isSelected = ViewModel != null && currentDate.Date == ViewModel.SelectedDate.Date;
                    bool isCurrentMonth = currentDate.Month == currentMonth.Month;
                    
                    // Устанавливаем стиль фона ячейки в зависимости от ее типа
                    cell.BackgroundColor = Colors.Transparent;
                    cell.Opacity = isCurrentMonth ? 1.0 : 0.5;
                    
                    // Создаем контейнер для содержимого дня
                    var contentStack = new VerticalStackLayout 
                    { 
                        Spacing = 4,
                        HorizontalOptions = LayoutOptions.Fill,
                        VerticalOptions = LayoutOptions.Fill,
                        Padding = new Thickness(2)
                    };
                    
                    // Номер дня
                    Label dayLabel;
                    
                    // Определяем стиль метки дня
                    if (isToday || isSelected)
                    {
                        // Для текущего или выбранного дня используем круглую рамку
                        var todayBorder = new Border
                        {
                            Style = isSelected 
                                ? (Style)Resources["SelectedCellStyle"] 
                                : (Style)Resources["TodayCellStyle"],
                            HorizontalOptions = LayoutOptions.Center
                        };
                        
                        dayLabel = new Label
                        {
                            Text = currentDate.Day.ToString(),
                            TextColor = isSelected ? Colors.White : (isCurrentMonth 
                                ? (Color)Application.Current.Resources["TextPrimaryColor"] 
                                : (Color)Application.Current.Resources["Gray500"]),
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center,
                            FontSize = 16,
                            FontAttributes = FontAttributes.Bold
                        };
                        
                        todayBorder.Content = dayLabel;
                        contentStack.Add(todayBorder);
                    }
                    else
                    {
                        // Для обычных дней просто отображаем номер
                        dayLabel = new Label
                        {
                            Text = currentDate.Day.ToString(),
                            Style = isCurrentMonth 
                                ? (Style)Resources["DayNumberStyle"] 
                                : (Style)Resources["OtherMonthDayStyle"],
                            HorizontalOptions = LayoutOptions.Center
                        };
                        contentStack.Add(dayLabel);
                    }
                    
                    // Пример добавления индикаторов событий - просто показываем случайные индикаторы для наглядности нового дизайна
                    // В реальном коде здесь должно быть получение событий из сервиса или через ViewModel
                    if (new Random().Next(10) > 6 && isCurrentMonth) // Примерно 30% дней будут иметь индикаторы
                    {
                        var indicatorsLayout = new HorizontalStackLayout
                        {
                            Spacing = 4,
                            HorizontalOptions = LayoutOptions.Center,
                            Margin = new Thickness(0, 2, 0, 0)
                        };
                        
                        // Случайное количество разных типов индикаторов
                        var rnd = new Random(currentDate.Day + currentDate.Month * 100); // Используем дату как seed для стабильных результатов
                        int eventTypes = rnd.Next(1, 5); // от 1 до 4 типов
                        
                        var styles = new (Style, int)[] 
                        {
                            ((Style)Resources["MedicationDotStyle"], 0),
                            ((Style)Resources["PhotoDotStyle"], 1),
                            ((Style)Resources["RestrictionDotStyle"], 2),
                            ((Style)Resources["InstructionDotStyle"], 3)
                        };
                        
                        var selectedStyles = styles
                            .OrderBy(x => rnd.Next()) // перемешиваем
                            .Take(eventTypes); // берем нужное количество
                        
                        foreach (var (style, _) in selectedStyles)
                        {
                            var dot = new BoxView { Style = style };
                            indicatorsLayout.Add(dot);
                        }
                        
                        contentStack.Add(indicatorsLayout);
                    }
                    
                    grid.Children.Add(contentStack);
                    
                    // Устанавливаем контекст привязки
                    cell.BindingContext = currentDate;
                    
                    // Переходим к следующему дню
                    currentDate = currentDate.AddDays(1);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in RefreshCalendarDays: {ex}");
            }
        }

        public void RefreshTapGestures()
        {
            foreach (var cell in CalendarGrid.Children.OfType<Frame>())
            {
                var tapGesture = cell.GestureRecognizers.OfType<TapGestureRecognizer>().FirstOrDefault();
                if (tapGesture != null)
                {
                    tapGesture.Tapped -= OnDayCellTapped;
                    tapGesture.Tapped += OnDayCellTapped;
                }
            }
        }

        private Grid CreateDayCell(CalendarDayViewModel day)
        {
            // Создаем ячейку для дня
            var cellGrid = new Grid
            {
                BindingContext = day,
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Star },
                    new RowDefinition { Height = GridLength.Auto }
                },
                RowSpacing = 2,
                Padding = new Thickness(0),
                BackgroundColor = Colors.Transparent,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };
            
            // Создаем фон ячейки
            var frame = new Frame
            {
                Style = (Style)Resources["CalendarCellStyle"],
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };
            cellGrid.Add(frame);
            
            // Создаем контейнер для содержимого дня
            var contentGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto }
                },
                RowSpacing = 4,
                Padding = new Thickness(2),
                BackgroundColor = Colors.Transparent
            };
            
            // Номер дня с выделением для текущего дня или выбранного дня
            if (day.IsSelected || day.IsToday)
            {
                var border = new Border
                {
                    Style = day.IsSelected ? 
                        (Style)Resources["SelectedCellStyle"] : 
                        (Style)Resources["TodayCellStyle"]
                };
                
                var dayLabel = new Label
                {
                    Text = day.DayText,
                    Style = (Style)Resources["DayNumberStyle"],
                    TextColor = day.IsSelected ? Colors.White : null
                };
                
                border.Content = dayLabel;
                contentGrid.Add(border, 0, 0);
            }
            else
            {
                var dayLabel = new Label
                {
                    Text = day.DayText,
                    Style = day.IsCurrentMonth ? 
                        (Style)Resources["DayNumberStyle"] : 
                        (Style)Resources["OtherMonthDayStyle"]
                };
                contentGrid.Add(dayLabel, 0, 0);
            }
            
            // Индикаторы событий
            if (day.HasEvents)
            {
                var indicatorsContainer = new HorizontalStackLayout
                {
                    Style = (Style)Resources["EventIndicatorsContainerStyle"]
                };
                
                if (day.HasMedication)
                    indicatorsContainer.Add(new BoxView { Style = (Style)Resources["MedicationDotStyle"] });
                
                if (day.HasPhoto)
                    indicatorsContainer.Add(new BoxView { Style = (Style)Resources["PhotoDotStyle"] });
                
                if (day.HasRestriction)
                    indicatorsContainer.Add(new BoxView { Style = (Style)Resources["RestrictionDotStyle"] });
                
                if (day.HasInstruction)
                    indicatorsContainer.Add(new BoxView { Style = (Style)Resources["InstructionDotStyle"] });
                
                contentGrid.Add(indicatorsContainer, 0, 1);
            }
            
            cellGrid.Add(contentGrid);
            
            // Добавляем обработчик нажатия
            var tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += (s, e) =>
            {
                if (ViewModel?.DaySelectedCommand?.CanExecute(day.Date) == true)
                {
                    ViewModel.DaySelectedCommand.Execute(day.Date);
                }
            };
            cellGrid.GestureRecognizers.Add(tapGestureRecognizer);
            
            return cellGrid;
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
} 