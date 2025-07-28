using System.Collections.Specialized;
using System.Linq;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Clinic.Common.Behaviors
{
    /// <summary>
    /// Automatically scrolls the attached CollectionView to the last item when new items are added.
    /// Keeps chat view in sync with newest messages.
    /// </summary>
    public class AutoScrollToBottomBehavior : Behavior<CollectionView>
    {
        private CollectionView? _collectionView;
        private INotifyCollectionChanged? _notifyCollection;

        protected override void OnAttachedTo(CollectionView bindable)
        {
            base.OnAttachedTo(bindable);
            _collectionView = bindable;
            Attach(bindable.ItemsSource);
            bindable.PropertyChanged += OnCollectionViewPropertyChanged;
        }

        protected override void OnDetachingFrom(CollectionView bindable)
        {
            Detach();
            bindable.PropertyChanged -= OnCollectionViewPropertyChanged;
            _collectionView = null;
            base.OnDetachingFrom(bindable);
        }

        private void OnCollectionViewPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ItemsView.ItemsSource) && _collectionView != null)
            {
                Detach();
                Attach(_collectionView.ItemsSource);
            }
        }

        private void Attach(object? itemsSource)
        {
            if (itemsSource is INotifyCollectionChanged notify)
            {
                _notifyCollection = notify;
                _notifyCollection.CollectionChanged += OnCollectionChanged;
            }
        }

        private void Detach()
        {
            if (_notifyCollection != null)
            {
                _notifyCollection.CollectionChanged -= OnCollectionChanged;
                _notifyCollection = null;
            }
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && _collectionView != null)
            {
                var items = _collectionView.ItemsSource as IEnumerable<object>;
                if (items == null) return;
                var lastItem = items.LastOrDefault();
                if (lastItem == null) return;
                _collectionView.Dispatcher.Dispatch(() =>
                {
                    _collectionView.ScrollTo(lastItem, position: ScrollToPosition.End, animate: true);
                });
            }
        }
    }
} 