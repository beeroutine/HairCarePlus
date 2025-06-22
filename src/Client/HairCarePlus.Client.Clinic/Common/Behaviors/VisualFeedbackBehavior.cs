using Microsoft.Maui.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HairCarePlus.Client.Clinic.Common.Behaviors;

public class VisualFeedbackBehavior : Behavior<Border>
{
    private static readonly ILogger<VisualFeedbackBehavior> _logger = NullLogger<VisualFeedbackBehavior>.Instance;

    protected override void OnAttachedTo(Border bindable)
    {
        base.OnAttachedTo(bindable);
        bindable.ChildAdded += OnFrameChildAdded;
        foreach (var gesture in bindable.GestureRecognizers.ToList())
            if (gesture is TapGestureRecognizer tap)
            {
                tap.Tapped -= OnTapped;
                tap.Tapped += OnTapped;
            }
    }

    protected override void OnDetachingFrom(Border bindable)
    {
        bindable.ChildAdded -= OnFrameChildAdded;
        foreach (var gesture in bindable.GestureRecognizers.ToList())
            if (gesture is TapGestureRecognizer tap)
                tap.Tapped -= OnTapped;
        base.OnDetachingFrom(bindable);
    }

    private void OnFrameChildAdded(object? sender, ElementEventArgs? args)
    {
        if (args?.Element is TapGestureRecognizer tap)
        {
            tap.Tapped -= OnTapped;
            tap.Tapped += OnTapped;
        }
    }

    private async void OnTapped(object? sender, EventArgs? e)
    {
        if (sender == null) return;
        Border? border = sender switch
        {
            TapGestureRecognizer tap when tap.Parent is Border b => b,
            Border b => b,
            _ => null
        };
        if (border == null) return;
        double original = border.Scale;
        await border.ScaleTo(0.95, 50, Easing.CubicOut);
        await border.ScaleTo(original, 150, Easing.SpringOut);
    }
} 