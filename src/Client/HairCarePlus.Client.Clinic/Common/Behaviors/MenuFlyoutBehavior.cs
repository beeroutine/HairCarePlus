using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Linq;

namespace HairCarePlus.Client.Clinic.Common.Behaviors;

public class MenuFlyoutBehavior : Behavior<Button>
{
    private Button? _button;
    public ObservableCollection<MenuFlyoutItem> MenuItems { get; } = new();

    protected override void OnAttachedTo(Button button)
    {
        base.OnAttachedTo(button);
        _button = button;
        _button.Clicked += OnButtonClicked;
    }

    protected override void OnDetachingFrom(Button button)
    {
        base.OnDetachingFrom(button);
        if (_button != null) _button.Clicked -= OnButtonClicked;
        _button = null;
    }

    private async void OnButtonClicked(object? sender, EventArgs e)
    {
        if (_button == null || !MenuItems.Any()) return;
        var result = await Application.Current!.MainPage!.DisplayActionSheet("Choose Action", "Cancel", null, MenuItems.Select(i => i.Text).ToArray());
        if (result == null || result == "Cancel") return;
        var selected = MenuItems.FirstOrDefault(i => i.Text == result);
        selected?.Command?.Execute(selected.CommandParameter);
    }
} 