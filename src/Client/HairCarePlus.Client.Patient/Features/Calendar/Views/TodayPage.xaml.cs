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
using MauiApp = Microsoft.Maui.Controls.Application;

namespace HairCarePlus.Client.Patient.Features.Calendar.Views
{
    public partial class TodayPage : ContentPage
    {
        private readonly ILogger<TodayPage> _logger;
        private TodayViewModel? _viewModel;
        private bool _isUpdatingFromScroll = false; // flag to avoid recursive scroll/selection updates

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
                
                // Binding should handle setting SelectedItem, but setting explicitly can be safer
                if ((DateSelectorView.SelectedItem as DateTime?) != _viewModel.SelectedDate)
                {
                     DateSelectorView.SelectedItem = _viewModel.SelectedDate;
                }
                
                // Set VisibleDate immediately via Dispatcher to update header when a date is TAPPED
                await MauiApp.Current.MainPage.Dispatcher.DispatchAsync(() =>
                {
                  _viewModel.VisibleDate = _viewModel.SelectedDate; 
                });
                
                // Scroll to selected date with animation
                _ = CenterSelectedDateAsync(); // Still scroll to center
                
                // Add delay and explicit state update after scroll completes
                await Task.Delay(200); // Slightly longer delay for animated scroll
        // UpdateVisibleItemStates(); // Удалено для устранения ошибки и избежания двойной подсветки
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
                // Ensure CollectionView has selected item AFTER ItemsSource populated
                if (DateSelectorView != null && (DateSelectorView.SelectedItem as DateTime?) != _viewModel.SelectedDate)
                {
                    DateSelectorView.SelectedItem = _viewModel.SelectedDate;
                }

                await _viewModel.EnsureLoadedAsync();
                _logger.LogInformation("EnsureLoadedAsync completed in OnAppearing");

                _viewModel.VisibleDate = _viewModel.SelectedDate;

                await Task.Delay(150); // Allow UI to render

                if (DateSelectorView != null)
                {
                    _ = CenterSelectedDateAsync();
                    await Task.Delay(50);

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

            _logger.LogInformation("TodayPage OnDisappearing completed");
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

        private class ClearHighlightAttachListener : Java.Lang.Object, Android.Views.View.IOnAttachStateChangeListener
        {
            public void OnViewAttachedToWindow(Android.Views.View? v) => ClearHighlight(v);
            public void OnViewDetachedFromWindow(Android.Views.View? v) { /* no-op */ }
        }
#endif
    }
} 