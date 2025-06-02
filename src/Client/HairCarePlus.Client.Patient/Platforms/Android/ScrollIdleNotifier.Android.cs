using System;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Controls;
using Android.Views;
using Android.Content;

namespace HairCarePlus.Client.Patient.Common.Behaviors
{
    public static partial class ScrollIdleNotifier
    {
#if ANDROID
        static partial void AttachInternal(CollectionView collectionView, Action onIdle)
        {
            if (collectionView == null || onIdle == null) return;

            void TryAttach()
            {
                if (collectionView.Handler?.PlatformView is RecyclerView rv)
                {
                    var listener = new IdleListener(onIdle);
                    rv.AddOnScrollListener(listener);
                    collectionView.SetValue(_listenerProperty, listener);
                }
            }

            collectionView.HandlerChanged += (_, __) => TryAttach();
            TryAttach();
        }

        private static readonly BindableProperty _listenerProperty =
            BindableProperty.CreateAttached("_scrollIdleListener", typeof(IdleListener), typeof(ScrollIdleNotifier), default(IdleListener));

        private sealed class IdleListener : RecyclerView.OnScrollListener
        {
            private readonly Action _cb;
            public IdleListener(Action cb) => _cb = cb;
            public override void OnScrollStateChanged(RecyclerView recyclerView, int newState)
            {
                if (newState == RecyclerView.ScrollStateIdle)
                    _cb?.Invoke();
            }
        }
#endif
    }
} 