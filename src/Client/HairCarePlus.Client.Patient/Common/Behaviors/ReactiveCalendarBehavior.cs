using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Maui.Controls;
using ReactiveUI;
using ReactiveUI.Maui;
using System.ComponentModel;
using System.Reactive;

namespace HairCarePlus.Client.Patient.Common.Behaviors
{
    /// <summary>
    /// Unified reactive behavior for calendar CollectionView that handles:
    /// - Selection state management
    /// - Smooth centering animation
    /// - Header synchronization with scrolling
    /// </summary>
    public class ReactiveCalendarBehavior : Behavior<CollectionView>
    {
        private CompositeDisposable _disposables = new();
        private CollectionView? _collectionView;
        
        #region Bindable Properties
        
        public static readonly BindableProperty SelectedDateProperty = BindableProperty.Create(
            nameof(SelectedDate), 
            typeof(DateTime), 
            typeof(ReactiveCalendarBehavior), 
            DateTime.Today,
            BindingMode.TwoWay);
            
        public DateTime SelectedDate
        {
            get => (DateTime)GetValue(SelectedDateProperty);
            set => SetValue(SelectedDateProperty, value);
        }
        
        public static readonly BindableProperty VisibleDateProperty = BindableProperty.Create(
            nameof(VisibleDate), 
            typeof(DateTime), 
            typeof(ReactiveCalendarBehavior), 
            DateTime.Today,
            BindingMode.TwoWay);
            
        public DateTime VisibleDate
        {
            get => (DateTime)GetValue(VisibleDateProperty);
            set => SetValue(VisibleDateProperty, value);
        }
        
        public static readonly BindableProperty CalendarDaysProperty = BindableProperty.Create(
            nameof(CalendarDays), 
            typeof(IList<DateTime>), 
            typeof(ReactiveCalendarBehavior));
            
        public IList<DateTime>? CalendarDays
        {
            get => (IList<DateTime>?)GetValue(CalendarDaysProperty);
            set => SetValue(CalendarDaysProperty, value);
        }
        
        #endregion
        
        protected override void OnAttachedTo(CollectionView bindable)
        {
            base.OnAttachedTo(bindable);
            _collectionView = bindable;
            
            // Inherit binding context
            BindingContext = bindable.BindingContext;
            bindable.BindingContextChanged += OnBindingContextChanged;
            
            SetupReactiveBindings();
        }
        
        protected override void OnDetachingFrom(CollectionView bindable)
        {
            bindable.BindingContextChanged -= OnBindingContextChanged;
            _disposables?.Dispose();
            _disposables = new CompositeDisposable();
            _collectionView = null;
            base.OnDetachingFrom(bindable);
        }
        
        private void OnBindingContextChanged(object? sender, EventArgs e)
        {
            if (sender is BindableObject bo)
            {
                BindingContext = bo.BindingContext;
            }
        }
        
        private void SetupReactiveBindings()
        {
            if (_collectionView == null) return;
            
            // Handle selection changes with smooth centering
            Observable.FromEventPattern<SelectionChangedEventArgs>(
                    h => _collectionView.SelectionChanged += h,
                    h => _collectionView.SelectionChanged -= h)
                .Throttle(TimeSpan.FromMilliseconds(100))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(args =>
                {
                    if (args.EventArgs.CurrentSelection.FirstOrDefault() is DateTime date)
                    {
                        SelectedDate = date;
                        CenterDate(date, animate: true);
                    }
                })
                .DisposeWith(_disposables);
            
            // Handle scrolling to update visible month
            Observable.FromEventPattern<ItemsViewScrolledEventArgs>(
                    h => _collectionView.Scrolled += h,
                    h => _collectionView.Scrolled -= h)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .Where(args => CalendarDays?.Count > 0)
                .Select(args => CalculateVisibleDate(args.EventArgs))
                .DistinctUntilChanged(date => new { date.Month, date.Year })
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(date => VisibleDate = date)
                .DisposeWith(_disposables);
            
            // Handle programmatic date selection
            Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                    h => PropertyChanged += h,
                    h => PropertyChanged -= h)
                .Where(ep => ep.EventArgs.PropertyName == nameof(SelectedDate))
                .Throttle(TimeSpan.FromMilliseconds(50))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    if (_collectionView?.SelectedItem is not DateTime currentSelection || 
                        currentSelection.Date != SelectedDate.Date)
                    {
                        _collectionView.SelectedItem = SelectedDate;
                        CenterDate(SelectedDate, animate: true);
                    }
                })
                .DisposeWith(_disposables);
            
            // Update visual states on visible cells
            var scrolledObservable = Observable.FromEventPattern<EventHandler<ItemsViewScrolledEventArgs>, ItemsViewScrolledEventArgs>(
                    h => _collectionView.Scrolled += h,
                    h => _collectionView.Scrolled -= h);

            var selectionObservable = Observable.FromEventPattern<EventHandler<SelectionChangedEventArgs>, SelectionChangedEventArgs>(
                    h => _collectionView.SelectionChanged += h,
                    h => _collectionView.SelectionChanged -= h);

            Observable.Merge(
                    scrolledObservable.Select(_ => Unit.Default),
                    selectionObservable.Select(_ => Unit.Default))
                .Throttle(TimeSpan.FromMilliseconds(50))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => UpdateVisualStates())
                .DisposeWith(_disposables);
            
            // Initial centering
            Observable.Timer(TimeSpan.FromMilliseconds(200))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => CenterDate(SelectedDate, animate: false))
                .DisposeWith(_disposables);
        }
        
        private DateTime CalculateVisibleDate(ItemsViewScrolledEventArgs args)
        {
            if (CalendarDays == null || CalendarDays.Count == 0) 
                return VisibleDate;
                
            var centerIndex = args.CenterItemIndex >= 0 
                ? args.CenterItemIndex 
                : (args.FirstVisibleItemIndex + args.LastVisibleItemIndex) / 2;
                
            if (centerIndex >= 0 && centerIndex < CalendarDays.Count)
            {
                return CalendarDays[centerIndex];
            }
            
            return VisibleDate;
        }
        
        private void CenterDate(DateTime date, bool animate)
        {
            if (_collectionView == null || CalendarDays == null) return;
            
            var index = CalendarDays.IndexOf(date.Date);
            if (index >= 0)
            {
#if ANDROID
                // Android-specific smooth scrolling
                if (_collectionView.Handler?.PlatformView is AndroidX.RecyclerView.Widget.RecyclerView recyclerView)
                {
                    var layoutManager = recyclerView.GetLayoutManager();
                    if (layoutManager is AndroidX.RecyclerView.Widget.LinearLayoutManager llm)
                    {
                        if (animate)
                        {
                            recyclerView.Post(() =>
                            {
                                var smoothScroller = new AndroidX.RecyclerView.Widget.LinearSmoothScroller(recyclerView.Context);
                                smoothScroller.TargetPosition = index;
                                layoutManager.StartSmoothScroll(smoothScroller);
                                
                                // Center after scroll
                                recyclerView.PostDelayed(() =>
                                {
                                    var view = layoutManager.FindViewByPosition(index);
                                    if (view != null)
                                    {
                                        var offset = (recyclerView.Width - view.Width) / 2;
                                        llm.ScrollToPositionWithOffset(index, offset);
                                    }
                                }, 300);
                            });
                        }
                        else
                        {
                            var offset = recyclerView.Width / 2;
                            llm.ScrollToPositionWithOffset(index, offset);
                        }
                        return;
                    }
                }
#endif
                // Default MAUI implementation
                _collectionView.ScrollTo(index, position: ScrollToPosition.Center, animate: animate);
            }
        }
        
        private void UpdateVisualStates()
        {
            if (_collectionView == null) return;
            
            var selectedDate = SelectedDate.Date;
            
#if ANDROID
            // Use Android-specific visible cells implementation
            if (_collectionView.Handler?.PlatformView is AndroidX.RecyclerView.Widget.RecyclerView recyclerView)
            {
                var layoutManager = recyclerView.GetLayoutManager();
                if (layoutManager != null)
                {
                    if (layoutManager is AndroidX.RecyclerView.Widget.LinearLayoutManager llm)
                    {
                        var first = llm.FindFirstVisibleItemPosition();
                        var last = llm.FindLastVisibleItemPosition();
                        
                        for (int i = first; i <= last && i >= 0; i++)
                        {
                            var holder = recyclerView.FindViewHolderForAdapterPosition(i);
                            if (holder?.ItemView != null)
                            {
                                var element = holder.ItemView.GetMauiElement();
                                if (element is VisualElement ve && ve.BindingContext is DateTime date)
                                {
                                    var state = date.Date == selectedDate ? "Selected" : "Normal";
                                    VisualStateManager.GoToState(ve, state);
                                }
                            }
                        }
                    }
                }
                return;
            }
#endif
            
            // Fallback for other platforms
            foreach (var child in _collectionView.LogicalChildren.OfType<VisualElement>())
            {
                if (child.BindingContext is DateTime date)
                {
                    var state = date.Date == selectedDate ? "Selected" : "Normal";
                    VisualStateManager.GoToState(child, state);
                }
            }
        }
    }
    
#if ANDROID
    internal static class AndroidExtensions
    {
        internal static VisualElement? GetMauiElement(this Android.Views.View view)
        {
            if (view.GetType().GetProperty("VirtualView")?.GetValue(view) is VisualElement ve)
                return ve;
                
            if (view is Android.Views.ViewGroup viewGroup)
            {
                for (int i = 0; i < viewGroup.ChildCount; i++)
                {
                    var result = viewGroup.GetChildAt(i)?.GetMauiElement();
                    if (result != null) return result;
                }
            }
            
            return null;
        }
    }
#endif
} 