using System.ComponentModel;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using System.Threading; // Added for CancellationTokenSource

namespace HairCarePlus.Client.Patient.Common.Behaviors
{
    /// <summary>
    /// Scrolls the CollectionView so that the currently SelectedItem is smoothly centered.
    /// Pure UI concern – no logging to keep it lightweight.
    /// </summary>
    public sealed class CenterOnSelectedBehavior : Behavior<CollectionView>
    {
        private CollectionView? _collection;
        private CancellationTokenSource? _debounceTokenSource;
        private bool _isInitializing = true;

        protected override void OnAttachedTo(CollectionView bindable)
        {
            base.OnAttachedTo(bindable);
            _collection = bindable;
            if (_collection == null) return;

            _collection.SelectionChanged += OnSelectionChanged;
            _collection.PropertyChanged += OnPropertyChanged;
            _collection.SizeChanged += OnSizeChanged; // Keep for layout changes like rotation
            _collection.HandlerChanged += OnHandlerChanged;

            RequestInitialScrollToCenter();
        }

        protected override void OnDetachingFrom(CollectionView bindable)
        {
            if (_collection != null)
            {
                _collection.SelectionChanged -= OnSelectionChanged;
                _collection.PropertyChanged -= OnPropertyChanged;
                _collection.SizeChanged -= OnSizeChanged;
                _collection.HandlerChanged -= OnHandlerChanged;
            }
            _debounceTokenSource?.Cancel();
            _debounceTokenSource?.Dispose();
            _collection = null;
            base.OnDetachingFrom(bindable);
        }

        private void OnSizeChanged(object? sender, EventArgs e)
        {
            // Re-center if size changes (e.g., rotation), might need initial scroll logic
            RequestInitialScrollToCenter();
        }

        // Animate scroll when the user changes selection so the movement feels natural.
        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) 
        {
            // SelectionChanged обычно вызывается при взаимодействии пользователя
            // Если выбор был программным (через SelectedItem binding), мы это уже обработали в OnPropertyChanged
            if (_collection?.SelectedItem != null && !_isInitializing)
            {
                // Всегда анимируем, если пользователь сменил выбор и мы не в фазе инициализации
                ScrollToCenter(animate: true);
            }
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CollectionView.ItemsSource))
            {
                // ItemsSource changed. Set initializing flag.
                // The actual initial scroll will be triggered by RequestInitialScrollToCenter 
                // or the first programmatic change to SelectedItem.
                _isInitializing = true;
                // ScrollToCenter(animate: false); // DO NOT scroll here immediately.
                
                // Reset the initializing flag after a delay, allowing initial selections/scrolls to complete.
                // Ensure this delay is longer than RequestInitialScrollToCenter's delay.
                _debounceTokenSource?.Cancel(); // Cancel any pending reset
                _debounceTokenSource?.Dispose();
                _debounceTokenSource = new CancellationTokenSource();
                var token = _debounceTokenSource.Token;
                Task.Delay(600, token).ContinueWith(t => 
                {
                    if (!t.IsCanceled) _isInitializing = false;
                }, token);
            }
            else if (e.PropertyName == nameof(CollectionView.SelectedItem))
            {
                // Programmatic change to SelectedItem (e.g., via binding)
                
                // If _isInitializing is true, it means ItemsSource just changed or we are in early setup.
                // In this case, let RequestInitialScrollToCenter (if it fires) or the natural end of initialization handle it.
                // We only want to trigger a scroll here if it's a programmatic change *after* initialization, 
                // or if it's the very first valid SelectedItem being set during initialization.
                if (_collection?.SelectedItem != null) // Only scroll if there's something to scroll to
                {
                    if (!_isInitializing)
                    {
                        ScrollToCenter(animate: true); // Animate if not initializing
                    }
                    else
                    {
                        // During initialization, a non-animated scroll is appropriate if this is the definitive SelectedItem.
                        // Rely on RequestInitialScrollToCenter which has a delay or allow this to proceed non-animated.
                        // To avoid multiple initial scrolls, let RequestInitialScrollToCenter be the main one.
                        // However, if SelectedItem is set *before* RequestInitialScrollToCenter's delay, we might need this.
                        // For now, let's assume RequestInitialScrollToCenter will handle it.
                        // Consider adding a log here to see when this branch is hit during init.
                    }
                }
            }
        }

        private void OnHandlerChanged(object? sender, EventArgs e)
        {
            // When native control is ready, good time for an initial center.
            // Also, ensure _isInitializing is true if we expect this to be an initial setup phase.
            _isInitializing = true; // Re-affirm that we are in an initialization phase
            RequestInitialScrollToCenter();
        }

        private void RequestInitialScrollToCenter()
        {
            _debounceTokenSource?.Cancel(); // Cancel previous (e.g. from ItemsSource changed or another HandlerChanged)
            _debounceTokenSource?.Dispose();
            _debounceTokenSource = new CancellationTokenSource();
            var token = _debounceTokenSource.Token;

            Task.Run(async () => {
                try
                {
                    // Delay to allow UI to settle, ItemsSource to be applied, and initial SelectedItem to be set by ViewModel
                    await Task.Delay(300, token); // Slightly longer delay: 300ms
                    if (token.IsCancellationRequested) return;

                    if (_collection?.SelectedItem != null) 
                    {
                        // This is the definitive initial scroll. Animate should be false.
                        // _isInitializing should ideally be true here.
                        ScrollToCenter(animate: false);
                    }
                    // After this initial scroll attempt, or if no item is selected, initialization phase for scrolling is over.
                    // The _isInitializing flag for general purposes is reset by ItemsSource change handler with a longer delay.
                }
                catch (TaskCanceledException)
                {
                    // Expected if token is cancelled
                }
            }, token);
        }

        private void ScrollToCenter(bool animate)
        {
            if (_collection == null) return;

            _collection.Dispatcher.Dispatch(() =>
            {
                if (_collection?.SelectedItem == null || _collection.ItemsSource == null)
                    return;

                int index = -1;
                if (_collection.ItemsSource is System.Collections.IList list)
                {
                    try
                    {
                        index = list.IndexOf(_collection.SelectedItem);
                    }
                    catch (ArgumentException) // Item might not be in the list temporarily during updates
                    {
                        index = -1; 
                    }
                }

                if (index < 0)
                {
                    // Fallback: if item is not found by index (e.g. list doesn't implement IList or item not present)
                    // but SelectedItem is valid.
                    if (_collection.SelectedItem != null)
                    {
                       _collection.ScrollTo(_collection.SelectedItem, position: ScrollToPosition.Center, animate: animate);
                    }
                }
                else
                {
                    _collection.ScrollTo(index, position: ScrollToPosition.Center, animate: animate);
                }
            });
        }
    }
} 