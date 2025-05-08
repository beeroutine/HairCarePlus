using System.ComponentModel;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Common.Behaviors
{
    /// <summary>
    /// Scrolls the CollectionView so that the currently SelectedItem is smoothly centered.
    /// Pure UI concern – no logging to keep it lightweight.
    /// </summary>
    public sealed class CenterOnSelectedBehavior : Behavior<CollectionView>
    {
        private CollectionView? _collection;

        protected override void OnAttachedTo(CollectionView bindable)
        {
            base.OnAttachedTo(bindable);
            _collection = bindable;
            _collection.SelectionChanged += OnSelectionChanged;
            _collection.PropertyChanged += OnPropertyChanged;
        }

        protected override void OnDetachingFrom(CollectionView bindable)
        {
            base.OnDetachingFrom(bindable);
            if (_collection != null)
            {
                _collection.SelectionChanged -= OnSelectionChanged;
                _collection.PropertyChanged -= OnPropertyChanged;
            }
            _collection = null;
        }

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => Center();

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(CollectionView.ItemsSource) or nameof(CollectionView.SelectedItem))
                Center();
        }

        private void Center()
        {
            if (_collection?.SelectedItem is not null)
            {
                _collection.ScrollTo(_collection.SelectedItem, position: ScrollToPosition.Center, animate: true);
            }
        }
    }
} 