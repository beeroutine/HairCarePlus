using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;

namespace HairCarePlus.Client.Patient.Features.Calendar.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HorizontalCalendarView : ContentView
    {
        public HorizontalCalendarView()
        {
            // Use code-behind until InitializeComponent is properly generated
            // InitializeComponent();
            
            // Create a simple temporary view until the XAML is properly generated
            Content = new StackLayout
            {
                Children =
                {
                    new Label
                    {
                        Text = "Horizontal Calendar",
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalOptions = LayoutOptions.Center,
                    },
                    new Label
                    {
                        Text = "Week of " + DateTime.Today.ToString("MMMM d, yyyy"),
                        FontSize = 16,
                        HorizontalOptions = LayoutOptions.Center,
                        Margin = new Thickness(0, 10, 0, 20)
                    },
                    CreateDaySelector()
                }
            };
        }
        
        private StackLayout CreateDaySelector()
        {
            var layout = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 10
            };
            
            // Create 7 days starting from today
            var startDate = DateTime.Today;
            
            for (int i = 0; i < 7; i++)
            {
                var date = startDate.AddDays(i);
                var dayFrame = new Frame
                {
                    CornerRadius = 8,
                    Padding = new Thickness(10, 5),
                    BackgroundColor = i == 0 ? Color.FromArgb("#FF5722") : Colors.Transparent,
                    BorderColor = Colors.LightGray,
                    Content = new StackLayout
                    {
                        Children =
                        {
                            new Label
                            {
                                Text = date.ToString("ddd"),
                                FontSize = 12,
                                TextColor = i == 0 ? Colors.White : Color.FromArgb("#757575"),
                                HorizontalOptions = LayoutOptions.Center
                            },
                            new Label
                            {
                                Text = date.Day.ToString(),
                                FontSize = 16,
                                FontAttributes = FontAttributes.Bold,
                                TextColor = i == 0 ? Colors.White : Color.FromArgb("#212121"),
                                HorizontalOptions = LayoutOptions.Center
                            }
                        }
                    }
                };
                
                layout.Children.Add(dayFrame);
            }
            
            return layout;
        }
    }
} 