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
using HairCarePlus.Client.Patient.Features.Calendar.Models;

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
            typeof(IList<CalendarDay>),
            typeof(ReactiveCalendarBehavior));
            
        public IList<CalendarDay>? CalendarDays
        {
            get => (IList<CalendarDay>?)GetValue(CalendarDaysProperty);
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
                    if (args.EventArgs.CurrentSelection.FirstOrDefault() is CalendarDay day)
                    {
                        SelectedDate = day.Date;
                        CenterDate(day.Date, animate: true);
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
                    if (_collectionView == null || CalendarDays == null) return;
                    var target = CalendarDays.FirstOrDefault(d => d.Date.Date == SelectedDate.Date);
                    if (target == null) return;
                    if (!_collectionView.SelectedItem?.Equals(target) ?? true)
                    {
                        _collectionView.SelectedItem = target;
                        CenterDate(SelectedDate, animate: true);
                    }
                })
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
                return CalendarDays[centerIndex].Date;
            }
            
            return VisibleDate;
        }
        
        private void CenterDate(DateTime date, bool animate)
        {
            if (_collectionView == null || CalendarDays == null) return;
            
            var index = -1;
            for(int i=0;i<CalendarDays.Count;i++)
            {
                if(CalendarDays[i].Date==date.Date)
                {
                    index = i;
                    break;
                }
            }

            if (index >= 0)
            {
                _collectionView.ScrollTo(index, position: ScrollToPosition.Center, animate: animate);
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