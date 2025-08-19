using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Clinic.Common.Behaviors;

/// <summary>
/// Lightweight diagnostic behavior that logs any tap on the attached element
/// and briefly animates it. Does not replace existing gesture recognizers.
/// </summary>
public sealed class TapProbeBehavior : Behavior<View>
{
    private TapGestureRecognizer? _probeTap;
    public string? ProbeTag { get; set; }

    private ILogger<TapProbeBehavior> ResolveLogger()
    {
        try
        {
            var services = Application.Current?.Handler?.MauiContext?.Services;
            var logger = services?.GetService<ILogger<TapProbeBehavior>>();
            return logger ?? NullLogger<TapProbeBehavior>.Instance;
        }
        catch
        {
            return NullLogger<TapProbeBehavior>.Instance;
        }
    }

    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);
        // Add passive tap recognizer that does not alter existing commands
        _probeTap = new TapGestureRecognizer();
        _probeTap.Tapped += OnTapped;
        bindable.GestureRecognizers.Add(_probeTap);
    }

    protected override void OnDetachingFrom(View bindable)
    {
        if (_probeTap != null)
        {
            _probeTap.Tapped -= OnTapped;
            bindable.GestureRecognizers.Remove(_probeTap);
        }
        base.OnDetachingFrom(bindable);
    }

    private async void OnTapped(object? sender, EventArgs e)
    {
        var logger = ResolveLogger();
        var element = (sender as TapGestureRecognizer)?.Parent as View
                       ?? sender as View;
        var tag = ProbeTag ?? element?.GetType().Name ?? "Unknown";
        var bcType = element?.BindingContext?.GetType().FullName ?? "<null>";
        logger.LogInformation("[TapProbe] Tag={Tag} Element={Element} BindingContext={BC}", tag, element?.GetType().Name, bcType);

        // Brief highlight for visual confirmation (non-intrusive)
        if (element is Border border)
        {
            var original = border.Opacity;
            await border.FadeTo(0.6, 50, Easing.CubicOut);
            await border.FadeTo(original, 120, Easing.CubicIn);
        }
        else if (element != null)
        {
            var original = element.Scale;
            await element.ScaleTo(0.97, 50, Easing.CubicOut);
            await element.ScaleTo(original, 100, Easing.CubicIn);
        }
    }
}


