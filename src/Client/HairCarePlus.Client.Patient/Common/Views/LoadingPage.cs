using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Common.Views
{
    public class LoadingPage : ContentPage
    {
        public LoadingPage()
        {
            // Упрощаем - всегда используем безопасный цвет
            BackgroundColor = Microsoft.Maui.Graphics.Colors.White;

            Content = new Grid
            {
                Children =
                {
                    new ActivityIndicator
                    {
                        IsRunning = true,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center,
                        Color = Colors.Gray,
                        WidthRequest = 40,
                        HeightRequest = 40
                    }
                }
            };
        }
    }
} 