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
        ApplySelectionState(); // initial apply (e.g., when page appears)
    }

    protected override void OnDetachingFrom(CollectionView bindable)
    {
        base.OnDetachingFrom(bindable);
        if (_collectionView != null)
        {
            _collectionView.SelectionChanged -= OnSelectionChanged;
            _collectionView.Scrolled -= OnScrolled;
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

    private void ApplySelectionState()
    {
        if (_collectionView == null) return;
        var selectedItem = _collectionView.SelectedItem;
        foreach (var visual in _collectionView.VisibleCells())
        {
            if (visual == null) continue;
            var state = Equals(visual.BindingContext, selectedItem) ? "Selected" : "Normal";
            VisualStateManager.GoToState(visual, state);
#if IOS
            // Remove default gray overlay for the corresponding native cell
            if (_collectionView.Handler?.PlatformView is UICollectionView ui)
            {
                foreach (var cell in ui.VisibleCells)
                {
                    cell.SelectedBackgroundView = new UIView { BackgroundColor = UIColor.Clear };
                }
            }
#endif
        }
    }
} 