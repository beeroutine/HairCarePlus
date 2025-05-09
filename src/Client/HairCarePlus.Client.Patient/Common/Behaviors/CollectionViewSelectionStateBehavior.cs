using System;
using System.Linq;
using Microsoft.Maui.Controls;
using HairCarePlus.Client.Patient.Common.Utils;
#if IOS
using UIKit;
#endif

namespace HairCarePlus.Client.Patient.Common.Behaviors;

/// <summary>
/// Ensures VisualState "Selected"/"Normal" is applied to items inside a CollectionView.
/// Designed specifically for DateSelectorView where automatic propagation is unreliable under MAUI 9.
/// Originally created for MAUI 9 when **CollectionView** didn't always propagate *Selected/Normal* visual
/// states to templated items. In MAUI 10 это уже исправлено, однако поведение остаётся полезным как
/// "защитная сетка" и для других CollectionView-ов, где требуется принудительно обновлять VSM
/// при появлении/скрытии ячеек (например, при горизонтальном прокручивании).
/// </summary>
public class CollectionViewSelectionStateBehavior : Behavior<CollectionView>
{
    private CollectionView? _collectionView;

    protected override void OnAttachedTo(CollectionView bindable)
    {
        base.OnAttachedTo(bindable);
        _collectionView = bindable;
        _collectionView.SelectionChanged += OnSelectionChanged;
        _collectionView.Scrolled += OnScrolled;
        _collectionView.HandlerChanged += OnHandlerChanged;
        ApplySelectionState(); // initial apply (e.g., when page appears)

        // Ensure state is applied once UI thread completed first layout
        _collectionView.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(30), () => ApplySelectionState());
    }

    protected override void OnDetachingFrom(CollectionView bindable)
    {
        base.OnDetachingFrom(bindable);
        if (_collectionView != null)
        {
            _collectionView.SelectionChanged -= OnSelectionChanged;
            _collectionView.Scrolled -= OnScrolled;
            _collectionView.HandlerChanged -= OnHandlerChanged;
        }
        _collectionView = null;
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ApplySelectionState();
    }

    private void OnScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        // When new cells become visible we need to refresh their states
        ApplySelectionState();
    }

    private void OnHandlerChanged(object? sender, EventArgs e)
    {
        ApplySelectionState();
    }

    private void ApplySelectionState()
    {
        if (_collectionView == null) return;
        var selectedItem = _collectionView.SelectedItem;

        // It's crucial to dispatch this to the UI thread, 
        // especially if ApplySelectionState might be called from a background thread (though unlikely here).
        _collectionView.Dispatcher.Dispatch(() =>
        {
            if (_collectionView == null) return; // Re-check after dispatch

            foreach (var visual in _collectionView.VisibleCells()) // VisibleCells is a helper extension method
            {
                if (visual == null) continue;
                var state = Equals(visual.BindingContext, selectedItem) ? "Selected" : "Normal";
                VisualStateManager.GoToState(visual, state);
            }

#if IOS
            // Attempt to remove default gray overlay for the corresponding native cell more robustly.
            if (_collectionView.Handler?.PlatformView is UIKit.UICollectionView uiCollectionView)
            {
                // Introduce a small delay to ensure our changes apply after any system default selection visuals.
                _collectionView.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(20), () =>
                {
                    if (uiCollectionView == null) return; // Re-check after dispatch
                    foreach (var cell in uiCollectionView.VisibleCells) // Native iOS VisibleCells
                    {
                        if (cell != null) 
                        {   
                            // Try setting to null first, then to a clear UIView.
                            cell.SelectedBackgroundView = null; 
                            cell.SelectedBackgroundView = new UIKit.UIView { BackgroundColor = UIKit.UIColor.Clear };
                        }
                    }
                });
            }
#endif
        });
    }
} 