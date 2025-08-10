using CommunityToolkit.Maui.Views;
using HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Progress.Views;

public class RestrictionDetailPopup : Popup
{
    public RestrictionDetailPopup(RestrictionTimer timer)
    {
                var layout = new VerticalStackLayout
        {
            Spacing = 12,
            WidthRequest = 280,
            Children =
            {
                new Label { Text = timer.Title, FontAttributes = FontAttributes.Bold, FontSize = 18, HorizontalOptions = LayoutOptions.Center },
                new Label { Text = $"Days remaining: {timer.DaysRemaining}", FontSize = 14, HorizontalOptions = LayoutOptions.Center },
                new Button
                {
                    Text = "Close",
                    Command = new Command(async () => await CloseAsync())
                }
            }
        };

        Content = layout;
    }
} 