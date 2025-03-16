using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;

namespace HairCarePlus.Client.Patient.Features.Calendar.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RestrictionTimersView : ContentView
    {
        public RestrictionTimersView()
        {
            // Create a simple restriction timers view
            var restrictions = new[]
            {
                new { Title = "No Alcohol", Description = "Avoid alcohol consumption to ensure proper healing", DaysLeft = 14 },
                new { Title = "No Intense Exercise", Description = "Avoid heavy physical activities", DaysLeft = 7 },
                new { Title = "No Swimming", Description = "Don't submerge your head in water", DaysLeft = 21 }
            };
            
            var layout = new StackLayout
            {
                Spacing = 16
            };
            
            // Add header
            layout.Children.Add(new Label
            {
                Text = "Active Restrictions",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#212121"),
                Margin = new Thickness(0, 0, 0, 8)
            });
            
            // Create horizontal scroll view for restriction cards
            var scrollContent = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Spacing = 16
            };
            
            foreach (var restriction in restrictions)
            {
                var card = new Frame
                {
                    CornerRadius = 8,
                    BorderColor = Color.FromArgb("#E0E0E0"),
                    BackgroundColor = Colors.White,
                    Padding = new Thickness(16),
                    WidthRequest = 220,
                    HeightRequest = 140,
                    Content = new StackLayout
                    {
                        Spacing = 8,
                        Children =
                        {
                            new Label
                            {
                                Text = restriction.Title,
                                FontSize = 16,
                                FontAttributes = FontAttributes.Bold,
                                TextColor = Color.FromArgb("#212121")
                            },
                            new Label
                            {
                                Text = restriction.Description,
                                FontSize = 14,
                                TextColor = Color.FromArgb("#757575"),
                                LineBreakMode = LineBreakMode.WordWrap
                            },
                            new StackLayout
                            {
                                Orientation = StackOrientation.Horizontal,
                                Margin = new Thickness(0, 10, 0, 0),
                                Children =
                                {
                                    new Label
                                    {
                                        Text = $"{restriction.DaysLeft} days remaining",
                                        FontSize = 14,
                                        FontAttributes = FontAttributes.Bold,
                                        TextColor = Color.FromArgb("#FF5722"),
                                        VerticalOptions = LayoutOptions.Center
                                    }
                                }
                            }
                        }
                    }
                };
                
                scrollContent.Children.Add(card);
            }
            
            var scrollView = new ScrollView
            {
                Orientation = ScrollOrientation.Horizontal,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
                Content = scrollContent
            };
            
            layout.Children.Add(scrollView);
            
            Content = new Frame
            {
                BorderColor = Color.FromArgb("#E0E0E0"),
                BackgroundColor = Colors.White,
                CornerRadius = 8,
                Padding = new Thickness(16),
                Content = layout
            };
        }
    }
} 