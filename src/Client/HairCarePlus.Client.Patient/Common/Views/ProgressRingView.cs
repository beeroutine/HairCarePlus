using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace HairCarePlus.Client.Patient.Common.Views
{
    public class ProgressRingView : ContentView
    {
        private readonly GraphicsView _graphicsView;
        private readonly ProgressRingDrawable _drawable;
        private float _animatedProgress = 0f;

        // Bindable Property for Progress (0.0 to 1.0)
        public static readonly BindableProperty ProgressProperty =
            BindableProperty.Create(nameof(Progress), typeof(double), typeof(ProgressRingView), 0.0d,
                                    propertyChanged: OnProgressChanged);

        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        // Bindable Property for Track Color
        public static readonly BindableProperty TrackColorProperty =
            BindableProperty.Create(nameof(TrackColor), typeof(Color), typeof(ProgressRingView), Colors.LightGrey,
                                    propertyChanged: OnColorChanged);

        public Color TrackColor
        {
            get => (Color)GetValue(TrackColorProperty);
            set => SetValue(TrackColorProperty, value);
        }

        // Bindable Property for Progress Color
        public static readonly BindableProperty ProgressColorProperty =
            BindableProperty.Create(nameof(ProgressColor), typeof(Color), typeof(ProgressRingView), Colors.Blue,
                                    propertyChanged: OnColorChanged);

        public Color ProgressColor
        {
            get => (Color)GetValue(ProgressColorProperty);
            set => SetValue(ProgressColorProperty, value);
        }

        // Bindable Property for Stroke Width
        public static readonly BindableProperty StrokeWidthProperty =
            BindableProperty.Create(nameof(StrokeWidth), typeof(double), typeof(ProgressRingView), 4.0d,
                                    propertyChanged: OnStrokeWidthChanged);

        public double StrokeWidth
        {
            get => (double)GetValue(StrokeWidthProperty);
            set => SetValue(StrokeWidthProperty, value);
        }

        public ProgressRingView()
        {
            _drawable = new ProgressRingDrawable();
            _graphicsView = new GraphicsView
            {
                Drawable = _drawable,
                BackgroundColor = Colors.Transparent
            };
            Content = _graphicsView;

            // Initialize drawable properties from defaults
            _drawable.TrackColor = TrackColor;
            _drawable.ProgressColor = ProgressColor;
            _drawable.StrokeWidth = (float)StrokeWidth;
        }

        private static void OnProgressChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ProgressRingView view && newValue is double newProgress)
            {
                view.AnimateProgress((float)newProgress);
            }
        }

        private static void OnColorChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ProgressRingView view && newValue is Color newColor)
            {
                if (view._drawable != null)
                {
                    // Determine which color property changed based on the BindableProperty
                    var property = GetPropertyFromValue(bindable, newValue, oldValue);
                    if (property == TrackColorProperty)
                        view._drawable.TrackColor = newColor;
                    else if (property == ProgressColorProperty)
                        view._drawable.ProgressColor = newColor;
                        
                    view._graphicsView?.Invalidate(); // Redraw with new color
                }
            }
        }

        private static void OnStrokeWidthChanged(BindableObject bindable, object oldValue, object newValue)
        {
             if (bindable is ProgressRingView view && newValue is double newWidth)
            {
                if (view._drawable != null)
                {
                    view._drawable.StrokeWidth = (float)newWidth;
                    view._graphicsView?.Invalidate(); // Redraw with new stroke width
                }
            }
        }

        // Helper to find which property triggered OnColorChanged (slightly hacky but works)
        private static BindableProperty GetPropertyFromValue(BindableObject bindable, object newValue, object oldValue)
        {
            if (Equals(bindable.GetValue(TrackColorProperty), newValue) && !Equals(bindable.GetValue(TrackColorProperty), oldValue))
                return TrackColorProperty;
            if (Equals(bindable.GetValue(ProgressColorProperty), newValue) && !Equals(bindable.GetValue(ProgressColorProperty), oldValue))
                 return ProgressColorProperty;
                 
            // Fallback or if old/new are same (initial set)
            if (Equals(bindable.GetValue(TrackColorProperty), newValue)) return TrackColorProperty;
            return ProgressColorProperty;
        }

        private void AnimateProgress(float targetProgress)
        {
            // Stop existing animation
            this.AbortAnimation("ProgressAnimation");

            var animation = new Animation(
                callback: v =>
                {
                    _animatedProgress = (float)v;
                    _drawable.Progress = _animatedProgress;
                    _graphicsView.Invalidate(); // Trigger redraw
                },
                start: _animatedProgress, // Start from current animated value
                end: targetProgress,
                easing: Easing.CubicOut);

            animation.Commit(this, "ProgressAnimation", 16, 350);
        }
    }
} 