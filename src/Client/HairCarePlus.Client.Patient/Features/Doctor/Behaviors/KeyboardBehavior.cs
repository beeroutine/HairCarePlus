using Microsoft.Maui.Controls;
using HairCarePlus.Client.Patient.Common.Behaviors;
using HairCarePlus.Client.Patient.Infrastructure.Services;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.PlatformConfiguration.WindowsSpecific;

namespace HairCarePlus.Client.Patient.Features.Doctor.Behaviors;

public class KeyboardBehavior : Behavior<CollectionView>
{
    private CollectionView _collectionView;
    private IKeyboardService _keyboardService;

    protected override void OnAttachedTo(CollectionView bindable)
    {
        base.OnAttachedTo(bindable);
        _collectionView = bindable;
        _keyboardService = ServiceHelper.GetService<IKeyboardService>();

        if (_keyboardService != null)
        {
            _keyboardService.KeyboardShown += OnKeyboardShown;
            _keyboardService.KeyboardHidden += OnKeyboardHidden;
        }
    }

    protected override void OnDetachingFrom(CollectionView bindable)
    {
        base.OnDetachingFrom(bindable);
        if (_keyboardService != null)
        {
            _keyboardService.KeyboardShown -= OnKeyboardShown;
            _keyboardService.KeyboardHidden -= OnKeyboardHidden;
        }
        _collectionView = null;
    }

    private void OnKeyboardShown(object sender, KeyboardEventArgs e)
    {
        if (_collectionView?.ItemsSource == null) return;

        Microsoft.Maui.Controls.Application.Current.Dispatcher.Dispatch(() =>
        {
            var items = _collectionView.ItemsSource.Cast<object>().ToList();
            if (items.Any())
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _collectionView.ScrollTo(items.Last(), position: ScrollToPosition.End, animate: false);
                });
            }
        });
    }

    private void OnKeyboardHidden(object sender, KeyboardEventArgs e)
    {
        if (_collectionView?.ItemsSource == null) return;

        Microsoft.Maui.Controls.Application.Current.Dispatcher.Dispatch(() =>
        {
            var items = _collectionView.ItemsSource.Cast<object>().ToList();
            if (items.Any())
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _collectionView.ScrollTo(items.Last(), position: ScrollToPosition.End, animate: false);
                });
            }
        });
    }
} 