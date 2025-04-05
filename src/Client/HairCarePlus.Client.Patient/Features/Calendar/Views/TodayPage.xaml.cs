using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Common.Behaviors;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Calendar.Views
{
    public partial class TodayPage : ContentPage
    {
        private TodayViewModel _viewModel;
        
        public TodayPage()
        {
            try
            {
                Debug.WriteLine("TodayPage constructor start");
                InitializeComponent();
                Debug.WriteLine("TodayPage InitializeComponent completed");
                
                _viewModel = ServiceHelper.GetService<TodayViewModel>();
                Debug.WriteLine("TodayViewModel retrieved from ServiceHelper");
                BindingContext = _viewModel;
                Debug.WriteLine("BindingContext set to TodayViewModel");
                
                // Добавляем обработчик изменения выбранного элемента в CollectionView
                daysCollection.SelectionChanged += OnDaysCollectionSelectionChanged;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in TodayPage constructor: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        protected override void OnAppearing()
        {
            try
            {
                Debug.WriteLine("TodayPage OnAppearing start");
                base.OnAppearing();
                
                // Refresh selected date when returning to this page
                if (_viewModel != null)
                {
                    DateTime currentDate = _viewModel.SelectedDate;
                    _viewModel.SelectedDate = currentDate;
                    
                    // Проверяем текущий выбранный элемент в CollectionView
                    Debug.WriteLine($"Current SelectedDate in ViewModel: {_viewModel.SelectedDate.ToShortDateString()}");
                    Debug.WriteLine($"Current SelectedItem in CollectionView: {daysCollection.SelectedItem}");
                    
                    // Программно обновляем визуальное состояние при загрузке страницы
                    // Это необходимо, так как VisualStateManager может не сработать автоматически 
                    // при первоначальной загрузке
                    UpdateSelectedDateVisualState(_viewModel.SelectedDate);
                    
                    // Принудительная загрузка данных
                    Task.Run(async () => 
                    {
                        await _viewModel.LoadTodayEventsAsync();
                        await _viewModel.LoadEventCountsForVisibleDaysAsync();
                        await _viewModel.CheckOverdueEventsAsync();
                        
                        // Добавляем отладочную информацию о загруженных событиях
                        var events = _viewModel.FlattenedEvents;
                        Debug.WriteLine($"Loaded {events?.Count ?? 0} events for date {currentDate.ToShortDateString()}");
                        if (events != null && events.Any())
                        {
                            foreach (var evt in events)
                            {
                                Debug.WriteLine($"Event: {evt.Title}, Type: {evt.EventType}, Time: {evt.Date.ToShortTimeString()}");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("No events found for selected date");
                        }
                    });
                    
                    Debug.WriteLine($"TodayPage OnAppearing refreshed selected date: {currentDate}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in TodayPage OnAppearing: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private void OnDaysCollectionSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is DateTime selectedDate)
            {
                Debug.WriteLine($"CollectionView selection changed to: {selectedDate.ToShortDateString()}");
                Debug.WriteLine($"ViewModel SelectedDate: {_viewModel.SelectedDate.ToShortDateString()}");
                
                // Найдем выбранный элемент и изменим его визуальное состояние программно
                UpdateSelectedDateVisualState(e.CurrentSelection.FirstOrDefault() as DateTime?);
            }
        }
        
        private void UpdateSelectedDateVisualState(DateTime? selectedDate)
        {
            if (selectedDate == null)
                return;
                
            try
            {
                Debug.WriteLine($"UpdateSelectedDateVisualState called for date: {selectedDate.Value.ToShortDateString()}");
                
                // Получаем контейнер выбранного элемента
                if (daysCollection.ItemTemplate == null)
                {
                    Debug.WriteLine("ItemTemplate is null");
                    return;
                }
                
                // Для каждого видимого элемента, проверяем, является ли он выбранным
                foreach (var container in daysCollection.GetVisualTreeDescendants().OfType<Grid>())
                {
                    if (container.BindingContext is DateTime containerDate)
                    {
                        bool isSelected = containerDate.Date == selectedDate.Value.Date;
                        Debug.WriteLine($"Container found for date: {containerDate.ToShortDateString()}, IsSelected: {isSelected}");
                        
                        var visualState = isSelected ? "Selected" : "Normal";
                        
                        // Применяем состояния ко всем элементам в иерархии
                        VisualStateManager.GoToState(container, visualState);
                        
                        // Находим Frame и Label внутри элемента
                        var frame = container.Children.FirstOrDefault(c => c is Frame) as Frame;
                        if (frame != null)
                        {
                            VisualStateManager.GoToState(frame, visualState);
                            
                            var label = frame.Content as Label;
                            if (label != null)
                            {
                                VisualStateManager.GoToState(label, visualState);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in UpdateSelectedDateVisualState: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private void OnCheckBoxChanged(object sender, CheckedChangedEventArgs e)
        {
            var checkBox = (CheckBox)sender;
            var parent = checkBox.Parent;
            VisualElement grid = null;
            
            // Traverse up the visual tree until we find a Grid
            while (parent != null && grid == null)
            {
                grid = parent as Grid;
                parent = parent.Parent;
            }
            
            if (grid != null)
            {
                // Set the visual state based on the checkbox value
                VisualStateManager.GoToState(grid, checkBox.IsChecked ? "ItemCompleted" : "ItemNormal");
                
                // Also set visual state for the checkbox itself
                VisualStateManager.GoToState(checkBox, checkBox.IsChecked ? "CheckboxCompleted" : "CheckboxNormal");
            }
            
            // Get the binding context and execute the command
            if (checkBox.BindingContext is CalendarEvent calendarEvent)
            {
                _viewModel.ToggleEventCompletionCommand.Execute(calendarEvent);
            }
        }
        
        private void OnCalendarDayLongPressed(object sender, EventArgs e)
        {
            try
            {
                if (sender is Grid grid && grid.BindingContext is DateTime date)
                {
                    _viewModel.OpenMonthCalendarCommand.Execute(date);
                    Debug.WriteLine($"OpenMonthCalendarCommand executed for date: {date.ToShortDateString()}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in OnCalendarDayLongPressed: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private void OnEventTapped(object sender, TappedEventArgs e)
        {
            try
            {
                if (sender is Grid grid && grid.BindingContext is CalendarEvent calendarEvent)
                {
                    _viewModel.ViewEventDetailsCommand.Execute(calendarEvent);
                    Debug.WriteLine($"ViewEventDetailsCommand executed for event: {calendarEvent.Title}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in OnEventTapped: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
} 