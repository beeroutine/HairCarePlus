#if IOS
using UIKit;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using System.Linq;

namespace HairCarePlus.Client.Patient.Platforms.iOS;

/// <summary>
/// Extension that removes the default gray selection highlight that iOS applies to <see cref="CollectionView"/> cells.
/// </summary>
public static class CollectionViewHighlightFix
{
    /// <summary>
    /// Disables the native selection background by replacing <c>SelectedBackgroundView</c> with a transparent view.
    /// Safe to call multiple times; works for already visible cells and for those that will appear later via <c>WillDisplayCell</c>.
    /// </summary>
    public static void DisableNativeHighlight(this CollectionView cv)
    {
        if (cv == null)
            return;

        void Apply(UICollectionView ui)
        {
            // For cells that are already on‑screen
            foreach (var cell in ui.VisibleCells)
                cell.SelectedBackgroundView = new UIView { BackgroundColor = UIColor.Clear };

            // Note: for reused cells, MAUI keeps our transparent SelectedBackgroundView
            // If in future grey appears again, consider creating a custom UICollectionViewDelegate.
        }

        // If handler is already created – apply immediately
        if (cv.Handler is CollectionViewHandler h && h.PlatformView is UICollectionView existing)
        {
            Apply(existing);
        }

        // Also hook to future handler creations (hot reload / re‑templating)
        cv.HandlerChanged += (_, __) =>
        {
            if (cv.Handler is CollectionViewHandler handler && handler.PlatformView is UICollectionView native)
            {
                Apply(native);
            }
        };
    }
}
#endif 