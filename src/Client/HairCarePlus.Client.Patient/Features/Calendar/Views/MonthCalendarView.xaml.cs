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
        
        // Событие для передачи информации об ошибке родительскому элементу
        public event EventHandler<Exception> ErrorOccurred;

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
                
                if (cells == null || !cells.Any())
                {
                    // Если ячейки не созданы, создаем их заново
                    CreateCalendarCells();
                    cells = CalendarGrid.Children.OfType<Frame>().ToList();
                    
                    if (cells == null || !cells.Any())
                    {
                        System.Diagnostics.Debug.WriteLine("Не удалось создать ячейки календаря");
                        // Вызываем событие с ошибкой
                        ErrorOccurred?.Invoke(this, new InvalidOperationException("Не удалось создать ячейки календаря"));
                        return;
                    }
                }
                
                foreach (var cell in cells)
                {
                    try
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
                        
                        // Обновляем BindingContext ячейки
                        cell.BindingContext = currentDate;
                        
                        // Добавляем содержимое в ячейку
                        grid.Children.Add(contentStack);
                        
                        // Переходим к следующему дню
                        currentDate = currentDate.AddDays(1);
                    }
                    catch (Exception cellEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка при обработке ячейки календаря: {cellEx.Message}");
                        // Вызываем событие с ошибкой ячейки, но продолжаем обработку
                        ErrorOccurred?.Invoke(this, cellEx);
                        // Продолжаем с другими ячейками
                        currentDate = currentDate.AddDays(1);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в RefreshCalendarDays: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"InnerException: {ex.InnerException.Message}");
                }
                
                // Вызываем событие с ошибкой
                ErrorOccurred?.Invoke(this, ex);
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
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto }  // Для длительных событий
                },
                RowSpacing = 2,
                Padding = new Thickness(0),
                BackgroundColor = Colors.Transparent,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };
            
            // Создаем фон ячейки с границей
            var cellBorder = new Border
            {
                Style = (Style)Resources["CalendarCellStyle"],
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };
            cellGrid.Add(cellBorder);
            
            // Создаем контейнер для содержимого дня
            var contentGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto }
                },
                RowSpacing = 4,
                Padding = new Thickness(4, 4, 4, 4),
                BackgroundColor = Colors.Transparent
            };
            
            // Номер дня с выделением для текущего дня или выбранного дня
            if (day.IsToday)
            {
                var todayBorder = new Border
                {
                    Style = (Style)Resources["TodayCellStyle"]
                };
                
                var dayLabel = new Label
                {
                    Text = day.DayText,
                    Style = (Style)Resources["TodayNumberStyle"]
                };
                
                todayBorder.Content = dayLabel;
                contentGrid.Add(todayBorder, 0, 0);
            }
            else if (day.IsSelected)
            {
                var selectedBorder = new Border
                {
                    Style = (Style)Resources["SelectedCellStyle"]
                };
                
                var dayLabel = new Label
                {
                    Text = day.DayText,
                    Style = (Style)Resources["DayNumberStyle"]
                };
                
                selectedBorder.Content = dayLabel;
                contentGrid.Add(selectedBorder, 0, 0);
            }
            else
            {
                var dayLabel = new Label
                {
                    Text = day.DayText,
                    Style = day.IsCurrentMonth ? 
                        (Style)Resources["DayNumberStyle"] : 
                        (Style)Resources["OtherMonthDayStyle"],
                    HorizontalOptions = LayoutOptions.Start,
                    Margin = new Thickness(8, 4, 0, 0)
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
                
                if (day.HasMedication && !day.IsPartOfMedicationRange)
                {
                    // Для приема лекарств используем синий круг
                    var indicator = new Border
                    {
                        BackgroundColor = Color.FromArgb("#42A5F5"),
                        WidthRequest = 6,
                        HeightRequest = 6,
                        Stroke = Colors.Transparent,
                        StrokeThickness = 0,
                        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                        {
                            CornerRadius = new CornerRadius(3)
                        },
                        HorizontalOptions = LayoutOptions.Center
                    };
                    indicatorsContainer.Add(indicator);
                }
                
                if (day.HasPhoto)
                {
                    // Для фото можно использовать иконку фотоаппарата, если она доступна
                    // или фиолетовый круг как запасной вариант
                    var photoIndicator = new Border
                    {
                        BackgroundColor = Color.FromArgb("#AB47BC"),
                        WidthRequest = 6,
                        HeightRequest = 6,
                        Stroke = Colors.Transparent,
                        StrokeThickness = 0,
                        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                        {
                            CornerRadius = new CornerRadius(3)
                        },
                        HorizontalOptions = LayoutOptions.Center
                    };
                    indicatorsContainer.Add(photoIndicator);
                }
                
                if (day.HasRestriction && !day.IsPartOfRestrictionRange)
                {
                    var restrictionIndicator = new Border
                    {
                        BackgroundColor = Color.FromArgb("#FF6B6B"),
                        WidthRequest = 6,
                        HeightRequest = 6,
                        Stroke = Colors.Transparent,
                        StrokeThickness = 0,
                        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                        {
                            CornerRadius = new CornerRadius(3)
                        },
                        HorizontalOptions = LayoutOptions.Center
                    };
                    indicatorsContainer.Add(restrictionIndicator);
                }
                
                if (day.HasInstruction)
                {
                    var instructionIndicator = new Border
                    {
                        BackgroundColor = Color.FromArgb("#66BB6A"),
                        WidthRequest = 6,
                        HeightRequest = 6,
                        Stroke = Colors.Transparent,
                        StrokeThickness = 0,
                        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                        {
                            CornerRadius = new CornerRadius(3)
                        },
                        HorizontalOptions = LayoutOptions.Center
                    };
                    indicatorsContainer.Add(instructionIndicator);
                }
                
                if (indicatorsContainer.Children.Any())
                {
                    contentGrid.Add(indicatorsContainer, 0, 1);
                }
            }
            
            cellGrid.Add(contentGrid);
            
            // Добавляем индикаторы длительных событий
            var rangesLayout = new VerticalStackLayout
            {
                Spacing = 2,
                Padding = new Thickness(0),
                HorizontalOptions = LayoutOptions.Fill
            };
            
            // Диапазон приема медикаментов (синий)
            if (day.IsPartOfMedicationRange)
            {
                var rangeStyle = (Style)Resources["MedicationRangeStyle"];
                Style shapeStyle;
                
                if (day.IsFirstDayInMedicationRange)
                    shapeStyle = (Style)Resources["RangeStartStyle"];
                else if (day.IsLastDayInMedicationRange)
                    shapeStyle = (Style)Resources["RangeEndStyle"];
                else
                    shapeStyle = (Style)Resources["RangeMiddleStyle"];
                
                var border = new Border
                {
                    Style = rangeStyle
                };
                MergeStyles(border, shapeStyle);
                
                rangesLayout.Add(border);
            }
            
            // Диапазон ограничений (красный) - более широкая полоса с текстом Restrictions
            if (day.IsPartOfRestrictionRange)
            {
                var rangeStyle = (Style)Resources["RestrictionRangeStyle"];
                Style shapeStyle;
                
                if (day.IsFirstDayInRestrictionRange)
                {
                    shapeStyle = (Style)Resources["RangeStartStyle"];
                    
                    // Если это первый день, добавляем подпись "Restrictions"
                    var restrictionLabel = new Label
                    {
                        Text = "Restrictions",
                        Style = (Style)Resources["RangeLabelStyle"]
                    };
                    
                    var restrictionContainer = new Grid();
                    
                    var restrictionBorder = new Border
                    {
                        Style = rangeStyle,
                        Content = restrictionLabel
                    };
                    MergeStyles(restrictionBorder, shapeStyle);
                    
                    rangesLayout.Add(restrictionBorder);
                }
                else if (day.IsLastDayInRestrictionRange)
                {
                    shapeStyle = (Style)Resources["RangeEndStyle"];
                    
                    var border = new Border
                    {
                        Style = rangeStyle
                    };
                    MergeStyles(border, shapeStyle);
                    
                    rangesLayout.Add(border);
                }
                else
                {
                    shapeStyle = (Style)Resources["RangeMiddleStyle"];
                    
                    var border = new Border
                    {
                        Style = rangeStyle
                    };
                    MergeStyles(border, shapeStyle);
                    
                    rangesLayout.Add(border);
                }
            }
            
            if (day.IsPartOfMedicationRange || day.IsPartOfRestrictionRange)
            {
                cellGrid.Add(rangesLayout, 0, 2);
            }
            
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

        // Вспомогательный метод для объединения стилей
        private void MergeStyles(Border target, Style additionalStyle)
        {
            foreach (var setter in additionalStyle.Setters)
            {
                if (setter is Setter setterInstance)
                {
                    target.SetValue(setterInstance.Property, setterInstance.Value);
                }
            }
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