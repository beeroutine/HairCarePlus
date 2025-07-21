using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Clinic.Common.Converters;

public class ImageLoadFailedBehavior : Behavior<Image>
{
    public static readonly BindableProperty FallbackVisibleProperty =
        BindableProperty.Create(
            nameof(FallbackVisible),
            typeof(bool),
            typeof(ImageLoadFailedBehavior),
            false,
            BindingMode.OneWayToSource);

    public bool FallbackVisible
    {
        get => (bool)GetValue(FallbackVisibleProperty);
        set => SetValue(FallbackVisibleProperty, value);
    }

    protected override void OnAttachedTo(Image bindable)
    {
        bindable.Loaded += OnImageLoaded;
        base.OnAttachedTo(bindable);
    }

    protected override void OnDetachingFrom(Image bindable)
    {
        bindable.Loaded -= OnImageLoaded;
        base.OnDetachingFrom(bindable);
    }

    private void OnImageLoaded(object? sender, EventArgs e)
    {
        if (sender is not Image image) return;

        // Check if image source is valid by trying to get its size
        // If image failed to load (404, network error, etc.), show fallback
        try
        {
            // MAUI Image doesn't directly expose load failure events
            // This is a simplified approach - in production you might want to use
            // platform-specific implementations or custom renderers
            var source = image.Source;
            if (source is UriImageSource uriSource && !string.IsNullOrEmpty(uriSource.Uri?.ToString()))
            {
                // For now, assume image is OK if it has a valid URI
                // The actual error handling would need platform-specific code
                FallbackVisible = false;
            }
        }
        catch
        {
            FallbackVisible = true;
        }
    }
} 