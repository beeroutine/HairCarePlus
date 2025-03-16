using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using System.Linq;

namespace HairCarePlus.Client.Patient.Features.Calendar.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MonthCalendarView : ContentView
    {
        public static readonly BindableProperty SelectedDateProperty = BindableProperty.Create(
            nameof(SelectedDate),
            typeof(DateTime),
            typeof(MonthCalendarView),
            DateTime.Today,
            BindingMode.TwoWay);
            
        public DateTime SelectedDate
        {
            get => (DateTime)GetValue(SelectedDateProperty);
            set => SetValue(SelectedDateProperty, value);
        }
        
        public MonthCalendarView()
        {
            // Use code-behind until XAML is properly generated
            Content = new Frame
            {
                BorderColor = Color.FromArgb("#E0E0E0"),
                BackgroundColor = Colors.White,
                CornerRadius = 8,
                Padding = new Thickness(16),
                Content = new StackLayout
                {
                    Spacing = 16,
                    Children =
                    {
                        new Label
                        {
                            Text = "Monthly Calendar",
                            FontSize = 18,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = Color.FromArgb("#212121")
                        },
                        CreateCalendarHeader(),
                        CreateCalendarGrid()
                    }
                }
            };
        }
        
        private Grid CreateCalendarHeader()
        {
            var header = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection(
                    Enumerable.Repeat(new ColumnDefinition(GridLength.Star), 7).ToArray()
                )
            };
            
            string[] days = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
            
            for (int i = 0; i < days.Length; i++)
            {
                var label = new Label
                {
                    Text = days[i],
                    FontSize = 14,
                    TextColor = Color.FromArgb("#757575"),
                    HorizontalOptions = LayoutOptions.Center
                };
                
                header.Add(label);
                Grid.SetColumn(label, i);
            }
            
            return header;
        }
        
        private Grid CreateCalendarGrid()
        {
            var grid = new Grid
            {
                RowSpacing = 10,
                ColumnSpacing = 10,
                ColumnDefinitions = new ColumnDefinitionCollection(
                    Enumerable.Repeat(new ColumnDefinition(GridLength.Star), 7).ToArray()
                ),
                RowDefinitions = new RowDefinitionCollection(
                    Enumerable.Repeat(new RowDefinition(GridLength.Auto), 6).ToArray()
                )
            };
            
            // Get first day of current month
            var date = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var firstDayOfMonth = date.Day;
            
            // Get the day of week (0 = Sunday, 1 = Monday, etc.)
            var firstDayOfWeek = (int)date.DayOfWeek;
            
            // Get number of days in the month
            var daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
            
            // Add days to the grid
            int day = 1;
            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 7; col++)
                {
                    // Skip cells before the first day of the month
                    if (row == 0 && col < firstDayOfWeek)
                    {
                        continue;
                    }
                    
                    // Stop after we've added all days in the month
                    if (day > daysInMonth)
                    {
                        break;
                    }
                    
                    // Create day cell
                    var isToday = day == DateTime.Today.Day && 
                                 date.Month == DateTime.Today.Month && 
                                 date.Year == DateTime.Today.Year;
                    
                    var dayFrame = new Frame
                    {
                        CornerRadius = 8,
                        Padding = new Thickness(8),
                        BackgroundColor = isToday ? Color.FromArgb("#FF5722") : Colors.Transparent,
                        BorderColor = isToday ? Colors.Transparent : Color.FromArgb("#E0E0E0"),
                        HeightRequest = 40,
                        Content = new Label
                        {
                            Text = day.ToString(),
                            TextColor = isToday ? Colors.White : Color.FromArgb("#212121"),
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center,
                            FontSize = 14
                        }
                    };
                    
                    grid.Add(dayFrame);
                    Grid.SetRow(dayFrame, row);
                    Grid.SetColumn(dayFrame, col);
                    
                    day++;
                }
            }
            
            return grid;
        }
    }
} 