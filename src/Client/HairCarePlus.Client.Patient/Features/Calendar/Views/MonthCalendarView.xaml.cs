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
        private List<TapGestureRecognizer> _dayTapGestures = new List<TapGestureRecognizer>();
        private CalendarViewModel? ViewModel => BindingContext as CalendarViewModel;
        private readonly Dictionary<DateTime, Frame> _dateCells = new();

        public MonthCalendarView()
        {
            InitializeComponent();
            CreateCalendarCells();
            
            // После загрузки данных создаем ячейки
            this.Loaded += (s, e) => {
                RefreshCalendarDays();
            };
            
            // При изменении размера представления пересоздаем ячейки
            this.SizeChanged += (s, e) => {
                if (Width > 0 && Height > 0)
                {
                    RefreshCalendarDays();
                }
            };
        }
        
        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged += (s, e) => {
                    if (e.PropertyName == nameof(ViewModel.CalendarDays) ||
                        e.PropertyName == nameof(ViewModel.SelectedDate) ||
                        e.PropertyName == nameof(ViewModel.CurrentMonthDate))
                    {
                        RefreshCalendarDays();
                    }
                };
                
                // Начальная отрисовка календаря
                RefreshCalendarDays();
            }
        }
        
        private void CreateCalendarCells()
        {
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
                Padding = new Thickness(0)
            };

            var dayLabel = new Label
            {
                Style = (Style)Resources["DayNumberStyle"]
            };

            var eventsStack = new StackLayout
            {
                Spacing = 2,
                Margin = new Thickness(0, 4, 0, 0)
            };

            cellGrid.Children.Add(dayLabel);
            cellGrid.Children.Add(eventsStack);
            Grid.SetRow(eventsStack, 1);

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
            if (sender is Frame frame && frame.BindingContext is DateTime selectedDate)
            {
                // Вызываем команду выбора даты из ViewModel
                if (BindingContext is CalendarViewModel viewModel)
                {
                    viewModel.DaySelectedCommand?.Execute(selectedDate);
                }
            }
        }

        public void UpdateCalendar(DateTime currentMonth)
        {
            var firstDayOfMonth = new DateTime(currentMonth.Year, currentMonth.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var firstDayOfCalendar = firstDayOfMonth.AddDays(-(int)firstDayOfMonth.DayOfWeek);

            var currentDate = firstDayOfCalendar;
            var cells = CalendarGrid.Children.OfType<Frame>().ToList();

            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                var grid = cell.Content as Grid;
                var dayLabel = grid?.Children.OfType<Label>().FirstOrDefault();

                if (dayLabel != null)
                {
                    dayLabel.Text = currentDate.Day.ToString();
                    cell.BindingContext = currentDate;

                    // Стилизация для дней не из текущего месяца
                    if (currentDate.Month != currentMonth.Month)
                    {
                        dayLabel.Opacity = 0.3;
                    }
                    else
                    {
                        dayLabel.Opacity = 1;
                    }

                    // Выделение текущего дня
                    if (currentDate.Date == DateTime.Today)
                    {
                        dayLabel.TextColor = (Color)Application.Current.Resources["Primary"];
                        dayLabel.FontAttributes = FontAttributes.Bold;
                    }
                }

                currentDate = currentDate.AddDays(1);
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

        private void RefreshCalendarDays()
        {
            try
            {
                // Очищаем текущие ячейки
                CalendarGrid.Children.Clear();
                _dayTapGestures.Clear();
                
                if (ViewModel == null || ViewModel.CalendarDays == null || ViewModel.CalendarDays.Count == 0)
                {
                    return;
                }
                
                // Добавляем ячейки для всех дней
                int row = 0;
                int col = 0;
                
                foreach (var dayViewModel in ViewModel.CalendarDays)
                {
                    var dayCell = CreateDayCell(dayViewModel);
                    
                    if (dayCell != null)
                    {
                        CalendarGrid.Add(dayCell, col, row);
                        
                        // Добавляем обработчик нажатия
                        var tapGesture = new TapGestureRecognizer();
                        tapGesture.Tapped += (s, e) => {
                            if (ViewModel.DaySelectedCommand?.CanExecute(dayViewModel.Date) == true)
                            {
                                ViewModel.DaySelectedCommand.Execute(dayViewModel.Date);
                            }
                        };
                        
                        dayCell.GestureRecognizers.Add(tapGesture);
                        _dayTapGestures.Add(tapGesture);
                    }
                    
                    // Переходим к следующей ячейке
                    col++;
                    if (col >= 7)
                    {
                        col = 0;
                        row++;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in RefreshCalendarDays: {ex.Message}");
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