#if IOS
using UIKit;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Platforms.iOS;

/// <summary>
/// Removes default grey selection background from CollectionView cells on iOS.
/// </summary>
public static class CollectionViewSelectionCleaner
{
    public static void Attach(CollectionView collectionView)
    {
        if (collectionView == null) return;

        collectionView.HandlerChanged += (_, _) =>
        {
            if (collectionView.Handler?.PlatformView is not UICollectionView uiCollection) return;

            ClearVisible(uiCollection);

            // During scroll and user interaction
            uiCollection.DraggingStarted += (_, __) => ClearVisible(uiCollection);
            uiCollection.Scrolled += (_, __) => ClearVisible(uiCollection);

            // After scroll or reload data
            uiCollection.DecelerationEnded += (_, __) => ClearVisible(uiCollection);
            uiCollection.DraggingEnded += (_, __) => ClearVisible(uiCollection);
        };
    }

    private static void Clear(UICollectionViewCell cell)
    {
        // Ensure the selected background is fully transparent
        cell.SelectedBackgroundView = new UIView { BackgroundColor = UIColor.Clear };

        // Also clear any residual background colours that iOS applies to the cell/content view
        cell.BackgroundView = null; // Remove automatically generated background view
        cell.BackgroundColor = UIColor.Clear;
        cell.ContentView.BackgroundColor = UIColor.Clear;

        // Fallback: explicitly clear CALayer background (helps on iOS 17 dark-mode)
        cell.Layer.BackgroundColor = UIColor.Clear.CGColor;
    }

    private static void ClearVisible(UICollectionView collectionView)
    {
        foreach (var cell in collectionView.VisibleCells)
            Clear(cell);
    }
}
#endif 