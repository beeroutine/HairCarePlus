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

#if ANDROID
using AndroidX.RecyclerView.Widget;
using Android.Views;
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
        // Android-specific implementation using RecyclerView
        if (collectionView.Handler?.PlatformView is AndroidX.RecyclerView.Widget.RecyclerView recyclerView)
        {
            var layoutManager = recyclerView.GetLayoutManager();
            if (layoutManager != null)
            {
                var llm = layoutManager as LinearLayoutManager;
                if (llm != null)
                {
                    var firstVisible = llm.FindFirstVisibleItemPosition();
                    var lastVisible = llm.FindLastVisibleItemPosition();
                    if (firstVisible >= 0 && lastVisible >= 0)
                    {
                        for (int i = firstVisible; i <= lastVisible; i++)
                        {
                            var viewHolder = recyclerView.FindViewHolderForAdapterPosition(i);
                            if (viewHolder?.ItemView != null)
                            {
                                // Get MAUI view from the native view
                                var platformView = viewHolder.ItemView;
                                if (platformView.GetType().GetProperty("VirtualView")?.GetValue(platformView) is VisualElement ve)
                                {
                                    yield return ve;
                                }
                                else
                                {
                                    // Alternative approach: search through view hierarchy
                                    var mauiContext = collectionView.Handler?.MauiContext;
                                    if (mauiContext != null)
                                    {
                                        var element = platformView.FindMauiView();
                                        if (element is VisualElement visualElement)
                                        {
                                            yield return visualElement;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    yield break;
                }
            }
            yield break; // Important: If Android implementation ran, stop here.
        }
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

#if ANDROID
    /// <summary>
    /// Helper to find MAUI view from Android native view
    /// </summary>
    private static VisualElement? FindMauiView(this Android.Views.View view)
    {
        if (view == null) return null;
        
        // Direct property check
        if (view.GetType().GetProperty("VirtualView")?.GetValue(view) is VisualElement ve)
            return ve;
            
        // Check if it's a ViewGroup and search children
        if (view is ViewGroup viewGroup)
        {
            for (int i = 0; i < viewGroup.ChildCount; i++)
            {
                var child = viewGroup.GetChildAt(i);
                var result = child?.FindMauiView();
                if (result != null) return result;
            }
        }
        
        return null;
    }
#endif
} 