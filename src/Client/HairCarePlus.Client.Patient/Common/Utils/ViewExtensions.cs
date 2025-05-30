using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging; // Added for potential logging
using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform; // Moved using to top level

#if IOS
using UIKit; // Added for iOS specific types
// using Microsoft.Maui.Platform; // Removed from here
#endif

namespace HairCarePlus.Client.Patient.Common.Utils;

/// <summary>
/// Provides extension methods for CollectionView, focused on accessing visual elements.
/// Originally part of CollectionViewVisualStateBehavior.
/// </summary>
internal static class CollectionViewExtensions
{
    // Logger can be added if detailed diagnostics are needed, requires DI or service locator pattern
    // private static ILogger _logger = ... 

    /// <summary>
    /// Finds the visual element corresponding to a specific data item within the CollectionView.
    /// Note: This method relies on iterating through currently visible cells.
    /// </summary>
    public static VisualElement? FindItemVisual(this CollectionView collectionView, object item)
    {
        if (collectionView == null || item == null) return null;

        foreach (var visual in collectionView.VisibleCells())
        {
            if (visual?.BindingContext != null && visual.BindingContext.Equals(item))
            {
                return visual;
            }
        }
        // _logger?.LogDebug("FindItemVisual did not find a visual element for item: {Item}", item);
        return null;
    }

    /// <summary>
    /// Gets an enumerable sequence of the currently visible visual elements within the CollectionView.
    /// Uses platform-specific implementation for iOS for better reliability.
    /// Falls back to iterating LogicalChildren, which might be less reliable with virtualization.
    /// </summary>
    public static IEnumerable<VisualElement?> VisibleCells(this CollectionView collectionView)
    {
        if (collectionView == null) yield break;

#if IOS
        if (collectionView.Handler?.PlatformView is UICollectionView uiCollectionView)
        {
            // _logger?.LogDebug("VisibleCells using iOS UICollectionView.IndexPathsForVisibleItems");
            foreach (var indexPath in uiCollectionView.IndexPathsForVisibleItems)
            {
                var cell = uiCollectionView.CellForItem(indexPath);
                // Access the MAUI VisualElement associated with the native cell
                var mauiView = cell?.GetMauiView() as VisualElement; 
                if (mauiView != null)
                {
                    yield return mauiView;
                }
                else
                {
                     // Fallback for potentially complex ItemTemplates or custom cells if GetMauiView fails
                     var first = cell?.ContentView?.Subviews.FirstOrDefault();
                     if (first != null && first.GetType().GetProperty("VirtualView")?.GetValue(first) is VisualElement veFromProp)
                     {
                         yield return veFromProp;
                     }
                }
            }
            yield break; // Important: If iOS implementation ran, stop here.
        }
#elif ANDROID
        // TODO: Add optimized Android implementation if needed, potentially using RecyclerView methods.
        // _logger?.LogWarning("VisibleCells on Android using fallback LogicalChildren traversal.");
        // Fall through to LogicalChildren for now
#endif

        // Fallback for other platforms or if Handler/PlatformView is null
        // _logger?.LogWarning("VisibleCells using fallback LogicalChildren traversal. This might be unreliable with virtualization.");
#pragma warning disable CS0618 // Type or member is obsolete
        foreach (var child in collectionView.LogicalChildren) // Changed from LogicalChildrenInternal
#pragma warning restore CS0618 // Type or member is obsolete
        {
            if (child is VisualElement ve)
            {
                yield return ve;
            }
        }
    }
    
#if IOS
    /// Helper to get the MAUI view from a native cell, simplifying access.
    private static IElement? GetMauiView(this UICollectionViewCell cell)
    {
        if (cell == null) return null;

        // Breadth‑first search through subviews looking for VirtualView property
        var queue = new Queue<UIView>();
        if (cell.ContentView != null)
            queue.Enqueue(cell.ContentView);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current.GetType().GetProperty("VirtualView")?.GetValue(current) is IElement element)
                return element;

            foreach (var sub in current.Subviews)
                queue.Enqueue(sub);
        }

        return null; // Nothing found
    }
#endif
} 