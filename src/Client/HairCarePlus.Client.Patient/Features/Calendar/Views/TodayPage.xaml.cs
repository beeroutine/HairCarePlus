using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using Microsoft.Maui.Handlers;
#if IOS
using HairCarePlus.Client.Patient.Platforms.iOS;
#endif
#if ANDROID
using Android.Views;
using AndroidX.RecyclerView.Widget;
#endif
using System.Threading;
using HairCarePlus.Client.Patient.Common.Utils;
using SkiaSharp.Extended.UI.Controls;
using SkiaSharp;
using HairCarePlus.Client.Patient.Common.Views;
using Microsoft.Maui.Graphics;
using System.Collections.Generic;

namespace HairCarePlus.Client.Patient.Features.Calendar.Views
{
    public partial class TodayPage : ContentPage
    {
        private readonly ILogger<TodayPage> _logger;
        private TodayViewModel? _viewModel;
        private bool _isUpdatingFromScroll = false; // flag to avoid recursive scroll/selection updates
        private Random _confettiRandom = new Random(); // Random generator for confetti

        public TodayPage(TodayViewModel viewModel, ILogger<TodayPage> logger)
        {
            try
            {
                _logger = logger; // assign first to allow early logging
                _logger.LogDebug("TodayPage constructor start");
                InitializeComponent();
                _logger.LogDebug("TodayPage InitializeComponent completed");
                
                _viewModel = viewModel;
                _logger.LogDebug("TodayViewModel retrieved from constructor");
                BindingContext = _viewModel;
                _logger.LogDebug("BindingContext set to TodayViewModel");
                
                _logger.LogInformation("TodayPage instance created");
                
                Microsoft.Maui.Controls.Application.Current.RequestedThemeChanged += OnThemeChanged;
                
                // iOS 17+/MAUI 9 already renders selection without gray overlay; no custom fix needed
                
#if IOS
                // Workaround: ensure iOS doesn't draw black overlay for selected CollectionView cell
                if (DateSelectorView != null)
                {
                    DateSelectorView.HandlerChanged += (_, __) =>
                    {
                        if (DateSelectorView.Handler?.PlatformView is UIKit.UICollectionView ui)
                        {
                            void ClearOverlay(UIKit.UICollectionView collection)
                            {
                                foreach (var cell in collection.VisibleCells)
                                    cell.SelectedBackgroundView = new UIKit.UIView { BackgroundColor = UIKit.UIColor.Clear };
                            }

                            // Apply immediately to currently visible cells
                            ClearOverlay(ui);

                            // Also clear overlay whenever user scrolls (new cells appear)
                            DateSelectorView.Scrolled += (_, __) => ClearOverlay(ui);
                            DateSelectorView.SelectionChanged += (_, __) => ClearOverlay(ui);
                        }
                    };
                }
                // iOS: remove gray background on cell selection
                Platforms.iOS.CollectionViewSelectionCleaner.Attach(DateSelectorView);
#endif
#if ANDROID
                // Android: remove default gray ripple/highlight on CollectionView selection
                if (DateSelectorView != null)
                {
                    DateSelectorView.HandlerChanged += (_, __) =>
                    {
                        if (DateSelectorView.Handler?.PlatformView is AndroidX.RecyclerView.Widget.RecyclerView rv)
                        {
                            // Listener that makes selected background transparent for each attached child view
                            rv.AddOnChildAttachStateChangeListener(new ClearHighlightAttachListener());

                            // Apply immediately for already attached views
                            for (int i = 0; i < rv.ChildCount; i++)
                            {
                                var child = rv.GetChildAt(i);
                                ClearHighlight(child);
                            }
                        }
                    };
                }
#endif

                // subscribe to completed event for confetti
                if (ProgressRingRef != null)
                {
                    ProgressRingRef.Completed += OnRingCompleted;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception in TodayPage constructor");
            }
        }
        
        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            // Clean up previous subscription if context changes
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            if (BindingContext is TodayViewModel newViewModel)
            {
                _viewModel = newViewModel; // Update the local reference
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
            else
            {
                _viewModel = null; // Clear local reference if context is not the expected ViewModel
            }
        }

        private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs? e)
        {
            if (e?.PropertyName == nameof(TodayViewModel.SelectedDate) && DateSelectorView != null && _viewModel != null)
            {
                _logger.LogDebug("ViewModel SelectedDate changed to {SelectedDate}, updating UI.", _viewModel.SelectedDate);
                
                // Используем индекс, чтобы гарантировать корректное применение выделения (особенно для DateTime-структур)
                if (_viewModel.CalendarDays is not null)
                {
                    var index = _viewModel.CalendarDays.IndexOf(_viewModel.SelectedDate.Date);
                    if (index >= 0)
                    {
                        var itemRef = _viewModel.CalendarDays[index];
                        if (!Equals(DateSelectorView.SelectedItem, itemRef))
                        {
                            DateSelectorView.SelectedItem = itemRef;
                        }
                    }
                }
                
                // Set VisibleDate immediately via Dispatcher to update header when a date is TAPPED
                var page = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page;
                if (page != null)
                {
                    await page.Dispatcher.DispatchAsync(() =>
                {
                  _viewModel.VisibleDate = _viewModel.SelectedDate; 
                });
                }
                
                // Scroll to selected date with animation - REMOVED, let CenterOnSelectedBehavior handle it
                // _ = CenterSelectedDateAsync(); 
                
                // Add delay and explicit state update after scroll completes
                await Task.Delay(200); // Slightly longer delay for animated scroll
        // UpdateVisibleItemStates(); // Удалено для устранения ошибки и избежания двойной подсветки
            }
            else if (e?.PropertyName == nameof(TodayViewModel.CompletionProgress)
                     && ProgressRingRef != null && _viewModel != null)
            {
                try
                {
                    // Progress is now a bindable property on SkiaProgressRing, direct animation call not needed from here
                    // The SkiaProgressRing will internally animate when its Progress property changes.
                    // We just need to ensure the ViewModel's CompletionProgress is bound to SkiaProgressRing.Progress in XAML.
                    // For now, let's assume the binding is set up correctly. If direct animation is still desired,
                    // SkiaProgressRing would need an AnimateToAsync method similar to the old one.
                    // Forcing a property update if binding doesn't trigger for some reason (defensive):
                    if (ProgressRingRef.Progress != _viewModel.CompletionProgress)
                    {
                        ProgressRingRef.Progress = _viewModel.CompletionProgress;
                    }
                }
                catch { /* ignore */ }
            }
        }
        
        /// <summary>
        /// Iterates through visible cells in DateSelectorView and applies Normal/Selected state.
        /// </summary>
        // private void UpdateVisibleItemStates()
        // {
        //      if (DateSelectorView == null || _viewModel == null) return;
        //      
        //      _logger.LogDebug("Updating visual states for visible items. Target SelectedDate: {SelectedDate}", _viewModel.SelectedDate);
        //      int updatedCount = 0;
        //      try
        //      {
        //         foreach (var visual in DateSelectorView.VisibleCells())
        //         {
        //             if (visual?.BindingContext is DateTime itemDate)
        //             {
        //                 var targetState = itemDate.Date == _viewModel.SelectedDate.Date ? "Selected" : "Normal";
        //                 // _logger.LogTrace("Applying state '{TargetState}' to visual for date {ItemDate}", targetState, itemDate.ToShortDateString());
        //                 VisualStateManager.GoToState(visual, targetState);
        //                 updatedCount++;
        //             }
        //             else
        //             {
        //                // _logger.LogWarning("Visible cell found with null or non-DateTime BindingContext: {BindingContext}", visual?.BindingContext);
        //             }
        //         }
        //         _logger.LogDebug("Finished updating visual states for {UpdatedCount} visible items.", updatedCount);
        //      }
        //      catch (Exception ex)
        //      {
        //          _logger.LogError(ex, "Error updating visible item states.");
        //      }
        // }

        private async Task CenterSelectedDateAsync()
        {
            if (DateSelectorView == null || _viewModel == null) return;
            try
            {
                // Просто прокручиваем к выбранной дате, визуальное выделение обеспечивается через DataTrigger в XAML 
                // и не зависит от анимации прокрутки
                DateSelectorView.ScrollTo(_viewModel.SelectedDate, position: ScrollToPosition.Center, animate: true);
                await Task.Yield(); // Просто сохраняем асинхронную подпись метода
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, "Error centering date");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _logger.LogInformation("TodayPage OnAppearing called");

            if (_viewModel == null)
            {
                _logger.LogWarning("ViewModel is null in OnAppearing, aborting.");
                return;
            }

            // (Re)Subscribe to events that were detached in OnDisappearing
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            if (ProgressRingRef != null)
            {
                ProgressRingRef.Completed += OnRingCompleted;
            }

            try
            {
                // Загружаем данные
                await _viewModel.EnsureLoadedAsync();
                _logger.LogInformation("EnsureLoadedAsync completed in OnAppearing");

                // Инициализируем прогресс кольца после загрузки данных
                if (ProgressRingRef != null && _viewModel != null)
                {
                    try
                    {
                        // Similar to OnViewModelPropertyChanged, assuming Progress is bound.
                        // Forcing initial update:
                        ProgressRingRef.Progress = _viewModel.CompletionProgress;
                        _logger.LogDebug("Initial progress animation completed: {Progress}", _viewModel.CompletionProgress);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during initial progress animation");
                    }
                }

                // Убеждаемся, что текущая дата выбрана и видима
                if (DateSelectorView != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        // Определяем индекс выбранной даты в источнике данных
                        if (_viewModel.CalendarDays is not null)
                        {
                            var index = _viewModel.CalendarDays.IndexOf(_viewModel.SelectedDate.Date);
                            if (index >= 0)
                            {
                                var itemRef = _viewModel.CalendarDays[index];
                                if (!Equals(DateSelectorView.SelectedItem, itemRef))
                                {
                                    DateSelectorView.SelectedItem = itemRef;
                                }
                            }
                            else
                            {
                                // Fallback: если элемент не найден (что маловероятно) – используем старую логику
                                DateSelectorView.SelectedItem = _viewModel.SelectedDate;
                            }
                        }
                        else
                        {
                            DateSelectorView.SelectedItem = _viewModel.SelectedDate;
                        }
                        
                        // Прокручиваем к выбранной дате без анимации
                        if (_viewModel.CalendarDays is not null)
                        {
                            var index = _viewModel.CalendarDays.IndexOf(_viewModel.SelectedDate.Date);
                            if (index >= 0)
                            {
                                DateSelectorView.ScrollTo(index, position: ScrollToPosition.Center, animate: false);
                            }
                            else
                            {
                                DateSelectorView.ScrollTo(_viewModel.SelectedDate, position: ScrollToPosition.Center, animate: false);
                            }
                        }
                        else
                        {
                            DateSelectorView.ScrollTo(_viewModel.SelectedDate, position: ScrollToPosition.Center, animate: false);
                        }
                        
                        // Принудительно обновляем состояние выделения
                        var selectedItem = DateSelectorView.GetVisualTreeDescendants()
                            .OfType<Grid>()
                            .FirstOrDefault(g => g.BindingContext is DateTime date && date.Date == _viewModel.SelectedDate.Date);
                            
                        if (selectedItem != null)
                        {
                            VisualStateManager.GoToState(selectedItem, "Selected");
                            _logger.LogInformation("Visual state updated for selected date: {Date}", _viewModel.SelectedDate.Date);
                        }
                    });
                }

                _logger.LogInformation("TodayPage OnAppearing completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TodayPage.OnAppearing");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _logger.LogInformation("TodayPage OnDisappearing called");

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            if (ProgressRingRef != null)
            {
                ProgressRingRef.Completed -= OnRingCompleted;
            }

            // Отписываемся от события смены темы
            Microsoft.Maui.Controls.Application.Current.RequestedThemeChanged -= OnThemeChanged;

            _logger.LogInformation("TodayPage OnDisappearing completed");
        }

        private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e)
        {
            try
            {
                _logger.LogInformation("Theme changed to: {Theme}", e.RequestedTheme);
                
                // Логируем состояние всех элементов в CollectionView
                if (DateSelectorView != null)
                {
                    var itemsLayout = DateSelectorView.ItemsLayout as LinearItemsLayout;
                    _logger.LogInformation("CollectionView orientation: {Orientation}", itemsLayout?.Orientation);
                    _logger.LogInformation("CollectionView items count: {Count}", DateSelectorView.ItemsSource?.Cast<object>().Count());
                    
                    // Проверяем цвета через VisualStateManager
                    var borders = DateSelectorView.GetVisualTreeDescendants()
                        .OfType<Border>()
                        .ToList();
                        
                    _logger.LogInformation("Found {Count} Border elements", borders.Count);
                    
                    foreach (var border in borders)
                    {
                        _logger.LogInformation("Border background color: {Color}, BindingContext type: {Type}", 
                            border.BackgroundColor,
                            border.BindingContext?.GetType().Name ?? "null");
                    }

                    // Проверяем текущие ресурсы
                    var lightColor = Microsoft.Maui.Controls.Application.Current.Resources["TaskCardBackgroundLight"];
                    var darkColor = Microsoft.Maui.Controls.Application.Current.Resources["TaskCardBackgroundDark"];
                    _logger.LogInformation("TaskCard colors in resources - Light: {Light}, Dark: {Dark}", lightColor, darkColor);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling theme change");
            }
        }

#if ANDROID
        // Helper to make any RecyclerView child view transparent to remove gray selection overlay
        private static void ClearHighlight(Android.Views.View? view)
        {
            if (view == null)
                return;

            view.Foreground = null;
            view.StateListAnimator = null;
            view.SetBackgroundColor(Android.Graphics.Color.Transparent);
        }

        private class ClearHighlightAttachListener : Java.Lang.Object, AndroidX.RecyclerView.Widget.RecyclerView.IOnChildAttachStateChangeListener
        {
            public void OnChildViewAttachedToWindow(Android.Views.View view) => ClearHighlight(view);
            public void OnChildViewDetachedFromWindow(Android.Views.View view) { /* no-op */ }
        }
#endif

        private void OnRingCompleted(object? sender, EventArgs e)
        {
            try
            {
                if (ConfettiView is SKConfettiView sk && sk.Width > 0 && sk.Height > 0)
                {
                    sk.IsAnimationEnabled = false;
                    sk.Systems.Clear();

                    var baseColors = new List<Microsoft.Maui.Graphics.Color>
                    {
                        Microsoft.Maui.Graphics.Colors.Gold,
                        Microsoft.Maui.Graphics.Colors.White,
                        Microsoft.Maui.Graphics.Colors.Cyan.WithAlpha(0.6f),
                        Microsoft.Maui.Graphics.Colors.LightPink.WithAlpha(0.6f),
                        Microsoft.Maui.Graphics.Colors.LightSkyBlue.WithAlpha(0.6f),
                        Microsoft.Maui.Graphics.Colors.PaleVioletRed.WithAlpha(0.6f)
                    };

                    int numberOfFireworks = _confettiRandom.Next(5, 8); // 5 to 7 fireworks

                    for (int i = 0; i < numberOfFireworks; i++)
                    {
                        // Select a subset of colors for this firework burst for variety
                        var currentFireworkColors = new SKConfettiColorCollection();
                        var shuffledColors = baseColors.OrderBy(c => _confettiRandom.Next()).ToList();
                        int colorCountForBurst = _confettiRandom.Next(2, 4); // 2 or 3 colors per burst
                        for(int cIdx = 0; cIdx < colorCountForBurst; cIdx++)
                        {
                            currentFireworkColors.Add(shuffledColors[cIdx % shuffledColors.Count]);
                        }

                        var shapes = new SKConfettiShapeCollection();
                        shapes.Add(new SKConfettiCircleShape()); // Small sparks (dots)

                        // Add a couple of streak shapes with random lengths
                        int streakCount = _confettiRandom.Next(2, 5); // 2 to 4 streaks per firework
                        for (int j = 0; j < streakCount; j++)
                        {
                            var streakPath = new SKPath();
                            streakPath.MoveTo(0, 0);
                            streakPath.LineTo(0, (float)(_confettiRandom.NextDouble() * 8.0 + 6.0)); // Length 6 to 14
                            shapes.Add(new SKConfettiPathShape(streakPath));
                        }

                        var physicsCollection = new SKConfettiPhysicsCollection();
                        int physicsTypeCount = _confettiRandom.Next(2, 4); // 2 or 3 physics types for variation
                        for (int k = 0; k < physicsTypeCount; k++)
                        {
                            physicsCollection.Add(new SKConfettiPhysics(
                                size: _confettiRandom.NextDouble() * 0.5 + 0.5,  // Size from 0.5 to 1.0
                                mass: _confettiRandom.NextDouble() * 0.4 + 0.6)); // Mass from 0.6 to 1.0
                        }
                        if (!physicsCollection.Any()) // Fallback
                        {
                            physicsCollection.Add(new SKConfettiPhysics(size: 0.7, mass: 0.7));
                        }

                        // Emitter position - random within the view
                        float emitterX = (float)(_confettiRandom.NextDouble() * sk.Width);
                        // Spawn in the upper 70% of the screen, avoiding edges too much
                        float emitterY = (float)(_confettiRandom.NextDouble() * (sk.Height * 0.70) + (sk.Height * 0.05));
                        
                        var fireworkSystem = new SKConfettiSystem
                        {
                            Emitter = SKConfettiEmitter.Burst(_confettiRandom.Next(25, 46)), // 25-45 particles per burst
                            EmitterBounds = SKConfettiEmitterBounds.Point(new Microsoft.Maui.Graphics.Point(emitterX, emitterY)),
                            Colors = currentFireworkColors,
                            Shapes = shapes,
                            Physics = physicsCollection,
                            Lifetime = (_confettiRandom.NextDouble() * 1.2 + 1.3), // Lifetime 1.3 to 2.5 seconds
                            MinimumInitialVelocity = _confettiRandom.Next(200, 351), // 200-350
                            MaximumInitialVelocity = _confettiRandom.Next(400, 701), // 400-700
                            MinimumRotationVelocity = -250,
                            MaximumRotationVelocity = 250,
                            Gravity = new Microsoft.Maui.Graphics.Point(0, _confettiRandom.Next(80, 131)), // Gravity 80-130
                            FadeOut = true,
                            StartAngle = 0,
                            EndAngle = 360
                        };
                        
                        if (fireworkSystem.MaximumInitialVelocity <= fireworkSystem.MinimumInitialVelocity)
                        {
                            fireworkSystem.MaximumInitialVelocity = fireworkSystem.MinimumInitialVelocity + _confettiRandom.Next(50, 101); // Add 50-100
                        }

                        sk.Systems.Add(fireworkSystem);
                    }

                    sk.IsAnimationEnabled = true;
                }
                else if (ConfettiView is SKConfettiView skView)
                {
                    _logger?.LogWarning("ConfettiView is not sized correctly for fireworks animation. Width: {Width}, Height: {Height}", skView.Width, skView.Height);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in OnRingCompleted for stylish micro fireworks confetti");
            }
        }
    }
} 