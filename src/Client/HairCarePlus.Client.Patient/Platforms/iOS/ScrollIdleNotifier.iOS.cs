using System;
using System.Linq;
using Foundation;
using Microsoft.Maui.Controls;
using UIKit;

namespace HairCarePlus.Client.Patient.Common.Behaviors
{
    public static partial class ScrollIdleNotifier
    {
#if IOS || MACCATALYST
        static partial void AttachInternal(CollectionView collectionView, Action onIdle)
        {
            if (collectionView == null || onIdle == null) return;

            void TryAttach()
            {
                if (collectionView.Handler?.PlatformView is UICollectionView uiCollection)
                {
                    var idleDelegate = new IdleDelegate(uiCollection, onIdle);
                    collectionView.SetValue(_delegateProperty, idleDelegate);
                }
            }

            collectionView.HandlerChanged += (_, __) => TryAttach();
            TryAttach();
        }

        private static readonly BindableProperty _delegateProperty =
            BindableProperty.CreateAttached("_scrollIdleDelegate", typeof(IdleDelegate), typeof(ScrollIdleNotifier), default(IdleDelegate));

        private sealed class IdleDelegate : UICollectionViewDelegateFlowLayout
        {
            private readonly Action _callback;
            private readonly WeakReference<NSObject> _prevDelegate;

            public IdleDelegate(UICollectionView collectionView, Action cb)
            {
                _callback = cb;
                _prevDelegate = new WeakReference<NSObject>(collectionView.Delegate as NSObject);
                collectionView.Delegate = this;
            }

            public override void DecelerationEnded(UIScrollView scrollView)
            {
                _callback?.Invoke();
                base.DecelerationEnded(scrollView);
            }

            public override void DraggingEnded(UIScrollView scrollView, bool willDecelerate)
            {
                if (!willDecelerate)
                    _callback?.Invoke();
                base.DraggingEnded(scrollView, willDecelerate);
            }

            public override void ScrollAnimationEnded(UIScrollView scrollView)
            {
                _callback?.Invoke();
                base.ScrollAnimationEnded(scrollView);
            }

            public override bool RespondsToSelector(ObjCRuntime.Selector sel)
            {
                if (base.RespondsToSelector(sel)) return true;
                if (_prevDelegate.TryGetTarget(out var prev))
                    return prev.RespondsToSelector(sel);
                return false;
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (disposing)
                {
                    _prevDelegate.SetTarget(null);
                }
            }
        }
#endif
    }
} 