using System.ComponentModel;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

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
            _collection.SizeChanged += OnSizeChanged;
            _collection.HandlerChanged += OnHandlerChanged;

            // Initial attempt once control is attached & ItemsSource possibly already set
            Device.BeginInvokeOnMainThread(Center);
        }

        protected override void OnDetachingFrom(CollectionView bindable)
        {
            base.OnDetachingFrom(bindable);
            if (_collection != null)
            {
                _collection.SelectionChanged -= OnSelectionChanged;
                _collection.PropertyChanged -= OnPropertyChanged;
                _collection.SizeChanged -= OnSizeChanged;
                _collection.HandlerChanged -= OnHandlerChanged;
            }
            _collection = null;
        }

        private void OnSizeChanged(object? sender, EventArgs e)
        {
            // Layout pass finished – try centering again (device rotation etc.)
            Center();
        }

        private async void DelayedCenter()
        {
            // Secondary attempt after UI thread settled (virtualization ready)
            await Task.Delay(40);
            Center();
        }

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => Center();

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(CollectionView.ItemsSource) or nameof(CollectionView.SelectedItem))
                Center();
        }

        private void OnHandlerChanged(object? sender, EventArgs e)
        {
            // When native control is ready, center again
            Center();
        }

        private void Center()
        {
            if (_collection?.SelectedItem is null || _collection.ItemsSource is null)
                return;

            // Determine index – safer with virtualization than item reference
            int index = -1;
            if (_collection.ItemsSource is System.Collections.IList list)
                index = list.IndexOf(_collection.SelectedItem);

            if (index < 0)
            {
                // Fallback to item scroll if index lookup failed
                _collection.Dispatcher.Dispatch(() =>
                    _collection.ScrollTo(_collection.SelectedItem, position: ScrollToPosition.Center, animate: true));
                return;
            }

            // Dispatch to ensure cell is materialised before scrolling (next UI cycle)
            _collection.Dispatcher.Dispatch(() =>
            {
                // Use index-based scroll for reliability with virtualization
                _collection.ScrollTo(index, position: ScrollToPosition.Center, animate: true);
            });

            // Fire secondary delayed attempt to reduce cases where first attempt happens too early
            DelayedCenter();
        }
    }
} 