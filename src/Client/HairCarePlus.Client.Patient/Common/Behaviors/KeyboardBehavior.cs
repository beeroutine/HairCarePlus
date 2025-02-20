using Microsoft.Maui.Controls;
using System.Runtime.CompilerServices;
using HairCarePlus.Client.Patient.Infrastructure.Services;

namespace HairCarePlus.Client.Patient.Common.Behaviors;

public class KeyboardBehavior : Behavior<ContentPage>
{
    private ContentPage _page;
    private IKeyboardService _keyboardService;
    private bool _isKeyboardShown;
    private Grid _mainGrid;
    private View _inputPanel;

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

        // Find the main grid and input panel
        if (_page.Content is Grid mainGrid)
        {
            _mainGrid = mainGrid;
            if (_mainGrid.Children.LastOrDefault() is View lastChild)
            {
                _inputPanel = lastChild;
            }
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
        _mainGrid = null;
        _inputPanel = null;
    }

    private void OnKeyboardShown(object sender, KeyboardEventArgs e)
    {
        if (_isKeyboardShown || _mainGrid == null || _inputPanel == null) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Only adjust the input panel's margin
            var margin = _inputPanel.Margin;
            _inputPanel.Margin = new Thickness(margin.Left, margin.Top, margin.Right, e.KeyboardHeight);
            _isKeyboardShown = true;
        });
    }

    private void OnKeyboardHidden(object sender, KeyboardEventArgs e)
    {
        if (!_isKeyboardShown || _mainGrid == null || _inputPanel == null) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Restore original margin for input panel
            var margin = _inputPanel.Margin;
            _inputPanel.Margin = new Thickness(margin.Left, margin.Top, margin.Right, 0);
            _isKeyboardShown = false;
        });
    }
} 