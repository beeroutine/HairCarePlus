using System.ComponentModel;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using System.Threading; // Added for CancellationTokenSource
#if ANDROID
using AndroidX.RecyclerView.Widget;
#endif

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
        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => ScrollToCenter(animate: true);

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CollectionView.ItemsSource))
            {
                // ItemsSource changed – initial positioning without animation while layout settles.
                ScrollToCenter(animate: false);
            }
            else if (e.PropertyName == nameof(CollectionView.SelectedItem))
            {
                // SelectedItem set programmatically (e.g., via ViewModel) – animate to keep UX consistent.
                ScrollToCenter(animate: true);
            }
        }

        private void OnHandlerChanged(object? sender, EventArgs e)
        {
            // When native control is ready, good time for an initial center.
            RequestInitialScrollToCenter();
        }

        private void RequestInitialScrollToCenter()
        {
            _debounceTokenSource?.Cancel();
            _debounceTokenSource?.Dispose();
            _debounceTokenSource = new CancellationTokenSource();
            var token = _debounceTokenSource.Token;

            Task.Run(async () => {
                try
                {
                    await Task.Delay(150, token); // Delay to allow UI to settle
                    if (!token.IsCancellationRequested)
                    {
                        ScrollToCenter(animate: false);
                    }
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

#if ANDROID
                // Android-specific handling for smoother scrolling
                if (_collection.Handler?.PlatformView is RecyclerView recyclerView)
                {
                    var layoutManager = recyclerView.GetLayoutManager();
                    if (layoutManager != null)
                    {
                        int itemIndex = GetItemIndex();
                        if (itemIndex >= 0)
                        {
                            // Use RecyclerView's native smooth scroll for better performance
                            if (animate)
                            {
                                var smoothScroller = new LinearSmoothScroller(recyclerView.Context)
                                {
                                    TargetPosition = itemIndex
                                };
                                smoothScroller.ComputeScrollVectorForPosition(itemIndex);
                                layoutManager.StartSmoothScroll(smoothScroller);
                                
                                // Additional centering after scroll completes
                                Task.Delay(300).ContinueWith(_ => 
                                {
                                    _collection.Dispatcher.Dispatch(() =>
                                    {
                                        if (layoutManager is LinearLayoutManager llm)
                                        {
                                            var offset = (recyclerView.Width / 2) - (recyclerView.GetChildAt(0)?.Width ?? 0) / 2;
                                            llm.ScrollToPositionWithOffset(itemIndex, offset);
                                        }
                                    });
                                });
                            }
                            else
                            {
                                // Instant scroll with centering
                                if (layoutManager is LinearLayoutManager llm)
                                {
                                    var offset = (recyclerView.Width / 2) - (recyclerView.GetChildAt(0)?.Width ?? 0) / 2;
                                    llm.ScrollToPositionWithOffset(itemIndex, offset);
                                }
                                else
                                {
                                    layoutManager.ScrollToPosition(itemIndex);
                                }
                            }
                            return;
                        }
                    }
                }
#endif

                // Default MAUI implementation for other platforms
                int index = GetItemIndex();
                
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
        
        private int GetItemIndex()
        {
            if (_collection?.ItemsSource is System.Collections.IList list)
            {
                try
                {
                    return list.IndexOf(_collection.SelectedItem);
                }
                catch (ArgumentException)
                {
                    return -1;
                }
            }
            return -1;
        }
    }
} 