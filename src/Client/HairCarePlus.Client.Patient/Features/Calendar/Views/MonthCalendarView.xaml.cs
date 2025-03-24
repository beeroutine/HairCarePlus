using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using System.Collections.Generic;
using System.Windows.Input;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Features.Calendar.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MonthCalendarView : ContentView
    {
        private readonly Frame[,] _dayCells = new Frame[6, 7];
        private readonly Label[,] _dayLabels = new Label[6, 7];
        private readonly HorizontalStackLayout[,] _eventIndicators = new HorizontalStackLayout[6, 7];
        private CalendarDayViewModel? _selectedDay = null;
        
        private CleanCalendarViewModel? ViewModel => BindingContext as CleanCalendarViewModel;

        public MonthCalendarView()
        {
            InitializeComponent();
            CreateCalendarCells();
            
            // Subscribe to binding context changes
            this.BindingContextChanged += OnBindingContextChanged;
        }
        
        private void OnBindingContextChanged(object sender, EventArgs e)
        {
            if (ViewModel != null)
            {
                // Subscribe to property changes in ViewModel
                ViewModel.PropertyChanged -= OnViewModelPropertyChanged; // Remove existing subscription if any
                ViewModel.PropertyChanged += (sender, args) => OnViewModelPropertyChanged(sender, args);
                UpdateCalendarDays();
            }
        }
        
        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CleanCalendarViewModel.CalendarDays) || 
                e.PropertyName == nameof(CleanCalendarViewModel.SelectedDate) ||
                e.PropertyName == nameof(CleanCalendarViewModel.CurrentMonthDate))
            {
                UpdateCalendarDays();
            }
        }
        
        private void CreateCalendarCells()
        {
            // Create calendar cells programmatically
            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 7; col++)
                {
                    // Create day number label
                    var dayLabel = new Label
                    {
                        Style = (Style)Resources["DayNumberStyle"],
                        Text = "",
                        HorizontalOptions = LayoutOptions.Center
                    };
                    _dayLabels[row, col] = dayLabel;
                    
                    // Create horizontal stack for event indicators
                    var eventIndicatorsStack = new HorizontalStackLayout
                    {
                        Style = (Style)Resources["EventIndicatorsContainerStyle"]
                    };
                    _eventIndicators[row, col] = eventIndicatorsStack;
                    
                    // Create vertical stack for day text and indicators
                    var stack = new VerticalStackLayout
                    {
                        Spacing = 2,
                        Children = { dayLabel, eventIndicatorsStack }
                    };
                    
                    // Create day cell frame
                    var dayCell = new Frame
                    {
                        Style = (Style)Resources["CalendarCellStyle"],
                        Content = stack,
                        InputTransparent = false,
                        BackgroundColor = Colors.Transparent
                    };
                    
                    // Create and add tap gesture recognizer
                    var tapGesture = new TapGestureRecognizer { CommandParameter = new CalendarCellInfo(row, col) };
                    tapGesture.Tapped += OnDayCellTapped;
                    dayCell.GestureRecognizers.Add(tapGesture);
                    
                    // Add cell to Grid
                    CalendarGrid.Add(dayCell, col, row);
                    _dayCells[row, col] = dayCell;
                }
            }
        }
        
        private void OnDayCellTapped(object? sender, TappedEventArgs e)
        {
            try
            {
                // Find which cell was tapped based on the sender
                var tappedFrame = sender as Frame;
                if (tappedFrame == null) return;

                // Find row and column of tapped cell
                int tappedRow = -1, tappedCol = -1;
                for (int row = 0; row < 6; row++)
                {
                    for (int col = 0; col < 7; col++)
                    {
                        if (_dayCells[row, col] == tappedFrame)
                        {
                            tappedRow = row;
                            tappedCol = col;
                            break;
                        }
                    }
                    if (tappedRow >= 0) break;
                }

                // If couldn't find the cell, exit
                if (tappedRow < 0 || tappedCol < 0) return;

                // Get day data from binding context
                var dayViewModel = tappedFrame.BindingContext as CalendarDayViewModel;
                if (dayViewModel == null || ViewModel == null) return;

                // Clear previous selection
                if (_selectedDay != null)
                {
                    var oldRow = GetRowForDay(_selectedDay);
                    var oldCol = GetColumnForDay(_selectedDay);
                    if (oldRow >= 0 && oldCol >= 0)
                    {
                        UpdateCellAppearance(oldRow, oldCol, _selectedDay, false);
                    }
                }

                // Select new cell
                _selectedDay = dayViewModel;
                UpdateCellAppearance(tappedRow, tappedCol, dayViewModel, true);

                // Update the selected date in the ViewModel
                ViewModel.SelectedDate = dayViewModel.Date;

                // Execute day selected command if available
                if (ViewModel.DaySelectedCommand?.CanExecute(dayViewModel.Date) == true)
                {
                    ViewModel.DaySelectedCommand.Execute(dayViewModel.Date);
                }

                // Force refresh through RefreshCommand
                if (ViewModel.RefreshCommand?.CanExecute(null) == true)
                {
                    ViewModel.RefreshCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling tap: {ex.Message}");
            }
        }

        private void UpdateCalendarDays()
        {
            if (ViewModel?.CalendarDays == null || ViewModel.CalendarDays.Count < 42)
                return;
                
            // Prepare data off the UI thread
            var updatesRequired = new List<(int Row, int Col, CalendarDayViewModel Day, bool IsSelected)>();
            CalendarDayViewModel? selectedDay = null;
            
            // Pre-calculate and collect updates
            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 7; col++)
                {
                    int index = row * 7 + col;
                    if (index < ViewModel.CalendarDays.Count)
                    {
                        var dayViewModel = ViewModel.CalendarDays[index];
                        bool isSelected = dayViewModel.IsSelected;
                        
                        // Add update to list
                        updatesRequired.Add((row, col, dayViewModel, isSelected));
                        
                        // If selected, save reference
                        if (isSelected)
                        {
                            selectedDay = dayViewModel;
                        }
                    }
                }
            }
            
            // Apply updates on UI thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    _selectedDay = selectedDay;
                    
                    // Apply all prepared updates
                    foreach (var update in updatesRequired)
                    {
                        // Update binding context and appearance
                        _dayCells[update.Row, update.Col].BindingContext = update.Day;
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
                // Update day label text
                _dayLabels[row, col].Text = dayViewModel.Date.Day.ToString();
                
                // Adjust visibility based on month
                if (dayViewModel.IsCurrentMonth)
                {
                    // Current month - fully visible
                    _dayLabels[row, col].Opacity = 1.0;
                    _dayLabels[row, col].TextColor = Colors.White;
                }
                else
                {
                    // Other months - semi-transparent
                    _dayLabels[row, col].Opacity = 0.5;
                    _dayLabels[row, col].TextColor = Colors.LightGray;
                }
                
                // Update event indicators
                UpdateEventIndicators(row, col, dayViewModel);
                
                // Set selected day highlighting
                Color selectedColor = Color.FromArgb("#6962AD");
                
                // Apply selection styling
                if (isSelected)
                {
                    _dayCells[row, col].BackgroundColor = selectedColor;
                    _dayLabels[row, col].TextColor = Colors.White;
                    _dayLabels[row, col].Opacity = 1.0;
                    _dayLabels[row, col].FontAttributes = FontAttributes.Bold;
                }
                else
                {
                    _dayCells[row, col].BackgroundColor = Colors.Transparent;
                    
                    // Special styling for today
                    if (dayViewModel.IsToday)
                    {
                        _dayLabels[row, col].FontAttributes = FontAttributes.Bold;
                        _dayLabels[row, col].TextColor = Color.FromArgb("#6962AD");
                    }
                    else
                    {
                        _dayLabels[row, col].FontAttributes = FontAttributes.None;
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
            if (row < 0 || row >= 6 || col < 0 || col >= 7 || dayViewModel == null)
                return;
                
            try
            {
                var indicatorsStack = _eventIndicators[row, col];
                indicatorsStack.Clear();
                
                // Debug logging
                System.Diagnostics.Debug.WriteLine($"Updating indicators for day {dayViewModel.Date:yyyy-MM-dd}, HasEvents: {dayViewModel.HasEvents}");
                
                if (!dayViewModel.HasEvents)
                {
                    indicatorsStack.IsVisible = false;
                    return;
                }
                
                // Убедимся, что у нас есть события для этого дня
                var events = ViewModel?.EventsForMonth
                    ?.Where(e => e.Date.Date == dayViewModel.Date.Date)
                    ?.ToList() ?? new List<CalendarEvent>();

                // Debug logging
                System.Diagnostics.Debug.WriteLine($"Found {events.Count} events for {dayViewModel.Date:yyyy-MM-dd}: {string.Join(", ", events.Select(e => e.EventType))}");

                // Если список событий пуст, скрываем индикаторы
                if (events.Count == 0)
                {
                    indicatorsStack.IsVisible = false;
                    return;
                }

                var eventTypes = events
                    .Select(e => e.EventType)
                    .Distinct()
                    .OrderBy(et => GetEventTypePriority(et))
                    .Take(3); // Показываем максимум 3 точки

                indicatorsStack.Clear();
                indicatorsStack.Spacing = 3; // Увеличиваем расстояние между точками
                
                foreach (var eventType in eventTypes)
                {
                    var dot = new BoxView
                    {
                        Style = GetStyleForEventType(eventType),
                        HeightRequest = 8, // Увеличиваем размер
                        WidthRequest = 8,  // Увеличиваем размер
                        CornerRadius = 4,  // Корректируем для нового размера
                        Margin = new Thickness(2, 0, 2, 0), // Больше места между точками
                        IsVisible = true,
                        Opacity = 1.0 // Убедимся что точки видны
                    };
                    indicatorsStack.Add(dot);
                    
                    // Debug logging
                    System.Diagnostics.Debug.WriteLine($"Added dot for event type: {eventType}");
                }

                // Убедимся что контейнер с точками виден
                indicatorsStack.IsVisible = true;
                indicatorsStack.HorizontalOptions = LayoutOptions.Center;
                indicatorsStack.Margin = new Thickness(0, 6, 0, 2); // Увеличиваем отступ сверху
                indicatorsStack.Spacing = 4; // Устанавливаем расстояние между точками
                indicatorsStack.MinimumHeightRequest = 10; // Обеспечиваем минимальную высоту
                
                // Проверяем родительские элементы
                var parentStack = indicatorsStack.Parent as VerticalStackLayout;
                if (parentStack != null)
                {
                    parentStack.IsVisible = true;
                    parentStack.Spacing = 6; // Увеличиваем расстояние в родительском контейнере
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating event indicators: {ex.Message}");
            }
        }
        
        private Style GetStyleForEventType(EventType eventType)
        {
            return eventType switch
            {
                EventType.Restriction => (Style)Resources["RestrictionDotStyle"],
                EventType.Medication => (Style)Resources["MedicationDotStyle"],
                EventType.Photo => (Style)Resources["PhotoDotStyle"],
                EventType.Instruction => (Style)Resources["InstructionDotStyle"],
                _ => (Style)Resources["EventDotBaseStyle"]
            };
        }
        
        private int GetEventTypePriority(EventType eventType)
        {
            return eventType switch
            {
                EventType.Restriction => 1,
                EventType.Medication => 2,
                EventType.Photo => 3,
                EventType.Instruction => 4,
                _ => 5
            };
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

        // Override OnParentSet to ensure gestures are properly set up
        protected override void OnParentSet()
        {
            base.OnParentSet();
            
            // Refresh tap gestures when the view is added to the visual tree
            if (Parent != null)
            {
                // Use Dispatcher instead of deprecated Device.StartTimer
                Dispatcher.DispatchAsync(async () => {
                    // Small delay to ensure layout is complete
                    await Task.Delay(100);
                    RefreshTapGestures();
                });
            }
        }

        // Add method to refresh tap gesture recognizers
        public void RefreshTapGestures()
        {
            try
            {
                // Update all cells to ensure they respond to taps
                for (int row = 0; row < 6; row++)
                {
                    for (int col = 0; col < 7; col++)
                    {
                        var dayCell = _dayCells[row, col];
                        
                        // Remove existing gestures
                        var oldGestures = dayCell.GestureRecognizers.ToList();
                        foreach (var gesture in oldGestures)
                        {
                            if (gesture is TapGestureRecognizer tap)
                            {
                                tap.Tapped -= OnDayCellTapped;
                                dayCell.GestureRecognizers.Remove(tap);
                            }
                        }
                        
                        // Add new gesture recognizer
                        var newTapGesture = new TapGestureRecognizer
                        {
                            CommandParameter = new CalendarCellInfo(row, col),
                            NumberOfTapsRequired = 1
                        };
                        newTapGesture.Tapped += OnDayCellTapped;
                        dayCell.GestureRecognizers.Add(newTapGesture);
                        
                        // Ensure input transparency is set properly
                        dayCell.InputTransparent = false;
                        
                        if (dayCell.Content is Layout layout)
                        {
                            layout.InputTransparent = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing tap gestures: {ex.Message}");
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