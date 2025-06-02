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
        private object? _lastProgrammaticSelection;

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
            if (_collection?.SelectedItem != null && !_isInitializing && 
                _collection.SelectedItem != _lastProgrammaticSelection)
            {
                // Это пользовательское взаимодействие - анимируем
                ScrollToCenter(animate: true);
            }
            // Сбрасываем флаг программного выбора
            _lastProgrammaticSelection = null;
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CollectionView.ItemsSource))
            {
                // ItemsSource changed – initial positioning without animation while layout settles.
                _isInitializing = true;
                ScrollToCenter(animate: false);
                // Сбрасываем флаг инициализации через небольшую задержку
                Task.Delay(500).ContinueWith(_ => _isInitializing = false);
            }
            else if (e.PropertyName == nameof(CollectionView.SelectedItem))
            {
                // SelectedItem изменен программно (через binding)
                // Запоминаем это значение, чтобы игнорировать последующий SelectionChanged
                _lastProgrammaticSelection = _collection?.SelectedItem;
                
                // Если это первоначальная загрузка - без анимации
                // Иначе - с анимацией (например, при нажатии кнопки "Сегодня")
                ScrollToCenter(animate: !_isInitializing);
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