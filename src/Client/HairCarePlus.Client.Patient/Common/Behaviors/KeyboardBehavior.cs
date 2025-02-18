using Microsoft.Maui.Controls;
using System.Runtime.CompilerServices;
using HairCarePlus.Client.Patient.Infrastructure.Services;

namespace HairCarePlus.Client.Patient.Common.Behaviors;

public class KeyboardBehavior : Behavior<ContentPage>
{
    private ContentPage _page;
    private IKeyboardService _keyboardService;
    private double _originalY = 0.0;
    private bool _isKeyboardShown;

    protected override void OnAttachedTo(ContentPage page)
    {
        base.OnAttachedTo(page);
        _page = page;
        _keyboardService = ServiceHelper.GetService<IKeyboardService>();

        if (_keyboardService != null)
        {
            _keyboardService.KeyboardShown += OnKeyboardShown;
            _keyboardService.KeyboardHidden += OnKeyboardHidden;
        }
    }

    protected override void OnDetachingFrom(ContentPage page)
    {
        base.OnDetachingFrom(page);
        if (_keyboardService != null)
        {
            _keyboardService.KeyboardShown -= OnKeyboardShown;
            _keyboardService.KeyboardHidden -= OnKeyboardHidden;
        }
        _page = null;
    }

    private void OnKeyboardShown(object sender, KeyboardEventArgs e)
    {
        if (_isKeyboardShown) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_page?.Content != null)
            {
                _originalY = _page.Content.Y;
                var shift = e.KeyboardHeight;
                _page.Content.TranslateTo(_page.Content.X, -shift, 100, Easing.Linear);
                _isKeyboardShown = true;
            }
        });
    }

    private void OnKeyboardHidden(object sender, KeyboardEventArgs e)
    {
        if (!_isKeyboardShown) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_page?.Content != null)
            {
                _page.Content.TranslateTo(_page.Content.X, _originalY, 100, Easing.Linear);
                _isKeyboardShown = false;
            }
        });
    }
} 