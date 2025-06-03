using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.Logging;
using System.Linq;
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
using HairCarePlus.Client.Patient.Common.Behaviors;

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

                // Attach idle notifier to trigger data load when scroll stops
                if (DateSelectorView != null)
                {
                    ScrollIdleNotifier.Attach(DateSelectorView, async () =>
                    {
                        if (_viewModel != null)
                        {
                            try
                            {
                                _logger.LogInformation("ScrollIdleNotifier: Scroll has settled. Current VM.SelectedDate: {SelectedDate}. Triggering LoadTodayEventsAsync.", _viewModel.SelectedDate);
                                await _viewModel.LoadTodayEventsAsync(); // Теперь без skipThrottling
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError(ex, "Error loading events after scroll idle");
                            }
                        }
                    });
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
                _logger.LogInformation("ViewModel SelectedDate changed to {SelectedDate}, updating UI.", _viewModel.SelectedDate);
                
                // Synchronize DateSelectorView.SelectedItem with ViewModel.SelectedDate
                // This ensures that CenterOnSelectedBehavior reacts to the ViewModel's state.
                if (_viewModel.CalendarDays is not null)
                {
                    var targetDate = _viewModel.SelectedDate.Date;
                    if (DateSelectorView.SelectedItem is DateTime currentItemDate && currentItemDate.Date == targetDate)
                    {
                        _logger.LogDebug("DateSelectorView.SelectedItem is already synchronized with ViewModel.SelectedDate ({TargetDate}). No UI update needed from OnViewModelPropertyChanged.", targetDate);
                    }
                    else
                    {
                        var index = _viewModel.CalendarDays.IndexOf(targetDate);
                        if (index >= 0)
                        {
                            var itemToSelect = _viewModel.CalendarDays[index];
                            _logger.LogDebug("Setting DateSelectorView.SelectedItem to {ItemToSelect} based on ViewModel.SelectedDate.", itemToSelect);
                            DateSelectorView.SelectedItem = itemToSelect;
                        }
                        else
                        {
                             _logger.LogWarning("Date {TargetDate} not found in CalendarDays. Cannot set DateSelectorView.SelectedItem.", targetDate);
                        }
                    }
                }
                
                // Set VisibleDate immediately via Dispatcher to update header when a date is TAPPED
                // This part is mostly for the month/year header, not directly for selection.
                // Consider if CollectionViewHeaderSyncBehavior should solely manage VisibleDate based on scroll.
                // For now, we keep it to ensure responsiveness of the header if a date is selected far off.
                await Microsoft.Maui.Controls.Application.Current.MainPage.Dispatcher.DispatchAsync(() =>
                {
                  if(_viewModel.VisibleDate.Date != _viewModel.SelectedDate.Date) // Update only if different
                  {
                    _viewModel.VisibleDate = _viewModel.SelectedDate; 
                    _logger.LogDebug("Updated ViewModel.VisibleDate to {VisibleDate} in OnViewModelPropertyChanged.", _viewModel.VisibleDate);
                  }
                });
                
                // NEW: Ensure the newly selected date is centered in the horizontal calendar
                try
                {
                    await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        DateSelectorView?.ScrollTo(_viewModel.SelectedDate, position: ScrollToPosition.Center, animate: true);
                    });
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error performing explicit ScrollTo in OnViewModelPropertyChanged");
                }
                
                // REMOVED Task.Delay and subsequent UpdateVisibleItemStates as it's problematic
                // VisualStateManager and DataTriggers should handle visual updates.
                // CenterOnSelectedBehavior handles scrolling.
                // ScrollIdleNotifier handles data loading after scroll.
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
                        // Эта часть кода гарантирует, что DateSelectorView.SelectedItem
                        // синхронизирован с _viewModel.SelectedDate.
                        // Это важно, так как CenterOnSelectedBehavior будет реагировать на SelectedItem.
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
                                // Fallback: если элемент не найден
                                if (!Equals(DateSelectorView.SelectedItem, _viewModel.SelectedDate))
                                {
                                    DateSelectorView.SelectedItem = _viewModel.SelectedDate;
                                }
                            }
                        }
                        else
                        {
                           if (!Equals(DateSelectorView.SelectedItem, _viewModel.SelectedDate))
                           {
                               DateSelectorView.SelectedItem = _viewModel.SelectedDate;
                           }
                        }
                        
                        // Удаляем явный вызов ScrollTo.
                        // CenterOnSelectedBehavior должен сам обработать изменение SelectedItem
                        // и выполнить прокрутку для центрирования.
                        // if (_viewModel.CalendarDays is not null)
                        // {
                        //     var index = _viewModel.CalendarDays.IndexOf(_viewModel.SelectedDate.Date);
                        //     if (index >= 0)
                        //     {
                        //         DateSelectorView.ScrollTo(index, position: ScrollToPosition.Center, animate: false);
                        //     }
                        //     else
                        //     {
                        //         DateSelectorView.ScrollTo(_viewModel.SelectedDate, position: ScrollToPosition.Center, animate: false);
                        //     }
                        // }
                        // else
                        // {
                        //     DateSelectorView.ScrollTo(_viewModel.SelectedDate, position: ScrollToPosition.Center, animate: false);
                        // }
                        
                        // Принудительно обновляем состояние выделения
                        var selectedItemVisual = DateSelectorView.GetVisualTreeDescendants()
                            .OfType<Grid>()
                            .FirstOrDefault(g => g.BindingContext is DateTime date && date.Date == _viewModel.SelectedDate.Date);
                            
                        if (selectedItemVisual != null)
                        {
                            VisualStateManager.GoToState(selectedItemVisual, "Selected");
                            _logger.LogInformation("Visual state updated for selected date: {Date} in OnAppearing", _viewModel.SelectedDate.Date);
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

        /// <summary>
        /// Event handler for CollectionView selection changes - replaces problematic TapGestureRecognizer
        /// </summary>
        private async void OnDateSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                _logger.LogInformation("OnDateSelectionChanged event fired. CurrentSelection count: {Count}", e.CurrentSelection?.Count ?? 0);
                
                if (e.CurrentSelection?.FirstOrDefault() is DateTime selectedDateFromEvent)
                {
                    _logger.LogInformation("Date selected via CollectionView: {SelectedDateFromEvent}", selectedDateFromEvent);
                    
                    if (_viewModel != null)
                    {
                        // Check if the ViewModel's SelectedDate is already what the event is reporting
                        // This is to prevent re-processing if a behavior changed SelectedItem,
                        // which updated ViewModel.SelectedDate via two-way binding, and then this event fired.
                        if (_viewModel.SelectedDate.Date == selectedDateFromEvent.Date)
                        {
                            _logger.LogInformation("OnDateSelectionChanged: selectedDateFromEvent ({SelectedDateFromEvent}) already matches ViewModel.SelectedDate. Skipping command execution.", selectedDateFromEvent.ToShortDateString());
                        }
                        else
                        {
                            // Call the SelectDateCommand manually
                            if (_viewModel.SelectDateCommand.CanExecute(selectedDateFromEvent))
                            {
                                _logger.LogInformation("Executing SelectDateCommand with date: {SelectedDateFromEvent}", selectedDateFromEvent);
                                _viewModel.SelectDateCommand.Execute(selectedDateFromEvent);
                                // После выполнения команды SelectedDate в ViewModel обновится.
                                // CenterOnSelectedBehavior подхватит это изменение и начнет анимацию.
                                // ScrollIdleNotifier затем вызовет LoadTodayEventsAsync.

                                // Явный вызов ScrollTo с анимацией – дополнительная гарантия, что дата окажется по центру,
                                // даже если CenterOnSelectedBehavior по какой-то причине не сработает.
                                try
                                {
                                    DateSelectorView?.ScrollTo(selectedDateFromEvent, position: ScrollToPosition.Center, animate: true);
                                }
                                catch (Exception ex)
                                {
                                    _logger?.LogError(ex, "Error in explicit ScrollTo after date tap");
                                }
                            }
                            else
                            {
                                _logger.LogWarning("SelectDateCommand.CanExecute returned false for date: {SelectedDateFromEvent}", selectedDateFromEvent);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("ViewModel is null in OnDateSelectionChanged");
                    }

                    // Загрузка задач произойдёт по событию Idle из ScrollIdleNotifier (iOS/Android)
                    // после того, как SelectedDate в ViewModel обновится и CenterOnSelectedBehavior завершит анимацию.

                    // Fallback: если анимация центрирования была минимальной и ScrollIdleNotifier не сработает,
                    // запустим подгрузку задач через небольшую задержку.
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(450);
                        try
                        {
                            if (_viewModel != null && _viewModel.SelectedDate.Date == selectedDateFromEvent.Date)
                            {
                                await _viewModel.LoadTodayEventsAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Delayed fallback LoadTodayEventsAsync failed");
                        }
                    });
                }
                else
                {
                    _logger.LogWarning("OnDateSelectionChanged called but no valid DateTime in CurrentSelection. CurrentSelection: {CurrentSelectionType}", 
                        e.CurrentSelection?.FirstOrDefault()?.GetType().Name ?? "null");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnDateSelectionChanged");
            }
        }

        private void OnEventCardTapped(object? sender, TappedEventArgs e)
        {
            _logger?.LogInformation("OnEventCardTapped triggered.");
            if (sender is not BindableObject bindableObject)
            {
                _logger?.LogWarning("OnEventCardTapped: Sender is not a BindableObject.");
                return;
            }

            if (bindableObject.BindingContext is not CalendarEvent calendarEvent)
            {
                _logger?.LogWarning("OnEventCardTapped: BindingContext of sender is not CalendarEvent. Actual type: {Type}", bindableObject.BindingContext?.GetType().FullName ?? "null");
                return;
            }

            _logger?.LogInformation("OnEventCardTapped: CalendarEvent '{Title}' tapped.", calendarEvent.Title);

            if (_viewModel == null)
            {
                _logger?.LogWarning("OnEventCardTapped: ViewModel is null.");
                return;
            }

            if (_viewModel.ToggleEventCompletionCommand.CanExecute(calendarEvent))
            {
                _logger?.LogInformation("OnEventCardTapped: Executing ToggleEventCompletionCommand for event '{Title}'.", calendarEvent.Title);
                _viewModel.ToggleEventCompletionCommand.Execute(calendarEvent);
            }
            else
            {
                _logger?.LogWarning("OnEventCardTapped: ToggleEventCompletionCommand cannot execute for event '{Title}'.", calendarEvent.Title);
            }
        }

        private void OnTaskLongPressed(object? sender, EventArgs e)
        {
            try
            {
                // Получаем элемент Border, на котором произошло событие
                if (sender is not Border border)
                {
                    _logger.LogWarning("OnTaskLongPressed: sender is not Border, but {Type}", sender?.GetType().Name ?? "null");
                    return;
                }

                // Получаем CalendarEvent из BindingContext
                if (border.BindingContext is not CalendarEvent calendarEvent)
                {
                    _logger.LogWarning("OnTaskLongPressed: BindingContext is not CalendarEvent, but {Type}", border.BindingContext?.GetType().Name ?? "null");
                    return;
                }

                _logger.LogInformation("OnTaskLongPressed: Task '{Title}' long-pressed, executing ToggleEventCompletionCommand", calendarEvent.Title);

                // Вызываем команду ViewModel
                if (_viewModel?.ToggleEventCompletionCommand?.CanExecute(calendarEvent) == true)
                {
                    _viewModel.ToggleEventCompletionCommand.Execute(calendarEvent);
                }
                else
                {
                    _logger.LogWarning("OnTaskLongPressed: ToggleEventCompletionCommand cannot execute or is null");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnTaskLongPressed");
            }
        }
    }
} 