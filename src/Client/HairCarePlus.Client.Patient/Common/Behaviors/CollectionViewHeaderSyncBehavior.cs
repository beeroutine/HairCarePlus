using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Common.Behaviors
{
    /// <summary>
    /// Behavior that tracks horizontal scrolling of a CollectionView containing dates
    /// and updates the bound <see cref="VisibleDate"/> property so that ViewModels can
    /// react (e.g. update month header). Implements small debounce to avoid UI jitter.
    /// </summary>
    public class CollectionViewHeaderSyncBehavior : Behavior<CollectionView>
    {
        private CollectionView? _associatedCollection;
        private CancellationTokenSource? _debounceCts;
        private const int DebounceDelayMs = 120;

        #region Bindable Properties

        public static readonly BindableProperty VisibleDateProperty = BindableProperty.Create(
            nameof(VisibleDate), typeof(DateTime), typeof(CollectionViewHeaderSyncBehavior), DateTime.Today, BindingMode.TwoWay);

        public DateTime VisibleDate
        {
            get => (DateTime)GetValue(VisibleDateProperty);
            set => SetValue(VisibleDateProperty, value);
        }

        public static readonly BindableProperty SelectableDatesProperty = BindableProperty.Create(
            nameof(SelectableDates), typeof(IList<DateTime>), typeof(CollectionViewHeaderSyncBehavior));

        /// <summary>
        /// Full list of dates used as ItemsSource. Required for index → date mapping.
        /// </summary>
        public IList<DateTime>? SelectableDates
        {
            get => (IList<DateTime>?)GetValue(SelectableDatesProperty);
            set => SetValue(SelectableDatesProperty, value);
        }

        #endregion

        protected override void OnAttachedTo(CollectionView bindable)
        {
            base.OnAttachedTo(bindable);
            _associatedCollection = bindable;
            bindable.Scrolled += OnCollectionViewScrolled;

            // Propagate BindingContext so that XAML bindings (e.g. VisibleDate ←→ ViewModel.VisibleDate)
            // work correctly. Without this, Behavior has no context and TwoWay update will not reach VM.
            BindingContext = bindable.BindingContext;
            bindable.BindingContextChanged += OnBindableBindingContextChanged;
        }

        protected override void OnDetachingFrom(CollectionView bindable)
        {
            bindable.Scrolled -= OnCollectionViewScrolled;
            bindable.BindingContextChanged -= OnBindableBindingContextChanged;
            _associatedCollection = null;
            _debounceCts?.Cancel();
            _debounceCts = null;
            base.OnDetachingFrom(bindable);
        }

        private void OnBindableBindingContextChanged(object? sender, EventArgs e)
        {
            if (sender is BindableObject bo)
            {
                BindingContext = bo.BindingContext;
            }
        }

        private void OnCollectionViewScrolled(object? sender, ItemsViewScrolledEventArgs e)
        {
            if (SelectableDates == null || SelectableDates.Count == 0) return;
            if (e.FirstVisibleItemIndex < 0 || e.LastVisibleItemIndex < 0) return;

            var centerIdx = e.CenterItemIndex >= 0 ? e.CenterItemIndex : (e.FirstVisibleItemIndex + e.LastVisibleItemIndex) / 2;
            if (centerIdx < 0 || centerIdx >= SelectableDates.Count) return;

            var candidate = SelectableDates[centerIdx];
            if (candidate.Month == VisibleDate.Month && candidate.Year == VisibleDate.Year) return;

            // Debounce header update to prevent rapid changes while scrolling
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(DebounceDelayMs, token);
                    if (token.IsCancellationRequested) return;

                    Device.BeginInvokeOnMainThread(() => VisibleDate = candidate);
                }
                catch (TaskCanceledException) { }
            }, token);
        }
    }
} 