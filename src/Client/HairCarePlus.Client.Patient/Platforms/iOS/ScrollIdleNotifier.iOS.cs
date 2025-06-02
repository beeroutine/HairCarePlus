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
                    var tracker = new IdleObserver(uiCollection, onIdle);
                    collectionView.SetValue(_trackerProperty, tracker);
                }
            }

            collectionView.HandlerChanged += (_, __) => TryAttach();
            TryAttach();
        }

        private static readonly BindableProperty _trackerProperty =
            BindableProperty.CreateAttached("_scrollIdleObserver", typeof(IdleObserver), typeof(ScrollIdleNotifier), default(IdleObserver));

        private sealed class IdleObserver : NSObject
        {
            private readonly NSObject _decelObserver;
            private readonly NSObject _dragObserver;
            private readonly Action _callback;

            public IdleObserver(UIScrollView scrollView, Action callback)
            {
                _callback = callback;

                var decelKey = new NSString("UIScrollViewDidEndDeceleratingNotification");
                var dragKey  = new NSString("UIScrollViewDidEndDraggingNotification");

                _decelObserver = NSNotificationCenter.DefaultCenter.AddObserver(decelKey, _ => _callback?.Invoke(), scrollView);

                _dragObserver = NSNotificationCenter.DefaultCenter.AddObserver(dragKey, note =>
                {
                    var willDecelerate = ((NSNumber?)note.UserInfo?[
                        new NSString("UIScrollViewWillDecelerateKey")])?.BoolValue ?? false;
                    if (!willDecelerate)
                        _callback?.Invoke();
                }, scrollView);
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (disposing)
                {
                    NSNotificationCenter.DefaultCenter.RemoveObserver(_decelObserver);
                    NSNotificationCenter.DefaultCenter.RemoveObserver(_dragObserver);
                }
            }
        }
#endif
    }
} 