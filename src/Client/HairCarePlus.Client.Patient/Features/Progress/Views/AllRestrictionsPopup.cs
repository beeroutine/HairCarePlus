using System.Collections.Generic;
using CommunityToolkit.Maui.Views;
using HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Progress.Views;

public class AllRestrictionsPopup : Popup
{
    public AllRestrictionsPopup(IReadOnlyList<RestrictionTimer> restrictions)
    {
                var list = new CollectionView
        {
            ItemsSource = restrictions,
            SelectionMode = SelectionMode.None,
            ItemTemplate = new DataTemplate(() =>
            {
                var title = new Label { FontAttributes = FontAttributes.Bold, FontSize = 14 };
                title.SetBinding(Label.TextProperty, nameof(RestrictionTimer.Title));

                var days = new Label { FontSize = 12 };
                days.SetBinding(Label.TextProperty, nameof(RestrictionTimer.DaysRemaining), stringFormat: "{0} days left");

                return new HorizontalStackLayout
                {
                    Spacing = 12,
                    Children = { title, days }
                };
            })
        };

        var closeButton = new Button { Text = "Close" };
        closeButton.Clicked += async (_, _) => await CloseAsync();

        Content = new VerticalStackLayout
        {
            Padding = 16,
            Spacing = 16,
            WidthRequest = 300,
            Children = { new Label { Text = "All Restrictions", FontAttributes = FontAttributes.Bold, FontSize = 18, HorizontalOptions = LayoutOptions.Center }, list, closeButton }
        };
    }
} 