using Microsoft.Maui.Controls;
using System;
using System.Linq;
using System.ComponentModel;

namespace HairCarePlus.Client.Patient.Features.Calendar.Behaviors
{
    public class DateSelectorBehavior : Behavior<CollectionView>
    {
        private CollectionView? _collectionView;
        private DateTime _lastScrolledDate;
        private object? _previousItemsSource;

        public static readonly BindableProperty SelectedDateProperty =
            BindableProperty.Create(nameof(SelectedDate), typeof(DateTime), typeof(DateSelectorBehavior), 
                DateTime.Today, BindingMode.TwoWay, propertyChanged: OnSelectedDateChanged);

        public static readonly BindableProperty ScrollToTargetDateProperty =
            BindableProperty.Create(nameof(ScrollToTargetDate), typeof(DateTime), typeof(DateSelectorBehavior), 
                DateTime.Today, BindingMode.TwoWay, propertyChanged: OnScrollToTargetDateChanged);

        public DateTime SelectedDate
        {
            get => (DateTime)GetValue(SelectedDateProperty);
            set => SetValue(SelectedDateProperty, value);
        }

        public DateTime ScrollToTargetDate
        {
            get => (DateTime)GetValue(ScrollToTargetDateProperty);
            set => SetValue(ScrollToTargetDateProperty, value);
        }

        private static void OnSelectedDateChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var behavior = (DateSelectorBehavior)bindable;
            if (behavior._collectionView != null && newValue is DateTime selectedDate)
            {
                behavior.UpdateSelectedItem(selectedDate);
            }
        }

        private static void OnScrollToTargetDateChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var behavior = (DateSelectorBehavior)bindable;
            if (behavior._collectionView != null && newValue is DateTime targetDate)
            {
                // First scroll to ensure the date is visible
                behavior.ScrollToDate(targetDate);
                
                // Also update the selected date to ensure the visual state is applied
                // Only do this if the dates don't match (the SelectedDate property binding might not have updated yet)
                if (behavior.SelectedDate.Date != targetDate.Date)
                {
                    behavior.SelectedDate = targetDate;
                }
                else
                {
                    // Force update the visual state even if the date is the same
                    behavior.UpdateSelectedItem(targetDate);
                }
            }
        }

        protected override void OnAttachedTo(CollectionView collectionView)
        {
            base.OnAttachedTo(collectionView);
            _collectionView = collectionView;
            
            // Apply initial selection
            if (SelectedDate != default)
            {
                UpdateSelectedItem(SelectedDate);
            }
            
            // Apply initial scroll position if needed
            if (ScrollToTargetDate != default && ScrollToTargetDate != _lastScrolledDate)
            {
                ScrollToDate(ScrollToTargetDate);
            }
            
            // Monitor for ItemsSource changes
            _collectionView.PropertyChanged += CollectionView_PropertyChanged;
            _previousItemsSource = _collectionView.ItemsSource;
        }

        protected override void OnDetachingFrom(CollectionView collectionView)
        {
            if (_collectionView != null)
            {
                _collectionView.PropertyChanged -= CollectionView_PropertyChanged;
            }
            
            _collectionView = null;
            _previousItemsSource = null;
            base.OnDetachingFrom(collectionView);
        }
        
        private void CollectionView_PropertyChanged(object? sender, PropertyChangedEventArgs? e)
        {
            if (e?.PropertyName == nameof(CollectionView.ItemsSource) && _collectionView != null)
            {
                // ItemsSource has changed
                if (!ReferenceEquals(_collectionView.ItemsSource, _previousItemsSource))
                {
                    _previousItemsSource = _collectionView.ItemsSource;
                    
                    // Re-apply selection when items source changes
                    if (SelectedDate != default)
                    {
                        UpdateSelectedItem(SelectedDate);
                    }
                }
            }
        }

        private void UpdateSelectedItem(DateTime selectedDate)
        {
            if (_collectionView?.ItemsSource == null) return;

            try 
            {
                var items = _collectionView.ItemsSource.Cast<object>().ToList();
                if (!items.Any()) return;

                foreach (var item in items)
                {
                    bool isSelected = item is DateTime date && date.Date == selectedDate.Date;
                    
                    var container = _collectionView.GetVisualTreeDescendants()
                        .OfType<Grid>()
                        .FirstOrDefault(g => g.BindingContext == item);

                    if (container != null)
                    {
                        var targetState = isSelected ? "Selected" : "Normal";
                        VisualStateManager.GoToState(container, targetState);
                        
                        if (isSelected)
                        {
                            ScrollToDate(selectedDate);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateSelectedItem: {ex.Message}");
            }
        }

        private void ScrollToDate(DateTime date)
        {
            if (_collectionView?.ItemsSource == null) return;

            try
            {
                var items = _collectionView.ItemsSource.Cast<object>().ToList();
                int index = -1;
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] is DateTime itemDate && itemDate.Date == date.Date)
                    {
                        index = i;
                        break;
                    }
                }
                
                if (index >= 0)
                {
                    _collectionView.Dispatcher.Dispatch(() => 
                    {
                        _collectionView.ScrollTo(index, position: ScrollToPosition.Center, animate: true);
                        _lastScrolledDate = date;
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ScrollToDate: {ex.Message}");
            }
        }
    }
} 