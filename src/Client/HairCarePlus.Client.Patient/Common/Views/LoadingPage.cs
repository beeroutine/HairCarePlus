using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Common.Views
{
    public class LoadingPage : ContentPage
    {
        public LoadingPage()
        {
            // Упрощаем - всегда используем безопасный цвет
            BackgroundColor = Microsoft.Maui.Graphics.Colors.White;

            var activityIndicator = new ActivityIndicator
            {
                IsRunning = true,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Color = Colors.Gray,
                WidthRequest = 40,
                HeightRequest = 40
            };

            var loadingLabel = new Label
            {
                Text = "Загрузка...",
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Start,
                TextColor = Colors.Gray,
                FontSize = 14,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var grid = new Grid
            {
                RowDefinitions = new RowDefinitionCollection
                {
                    new RowDefinition { Height = GridLength.Star },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Star }
                }
            };

            grid.Add(activityIndicator, 0, 1);
            grid.Add(loadingLabel, 0, 2);

            Content = grid;
        }
    }
} 