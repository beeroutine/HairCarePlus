using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Core; // for AnimationExtensions if needed
using Microsoft.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using HairCarePlus.Client.Patient.Common.Utils; // For ToSKColor extension
using Microsoft.Maui.Graphics; // For Maui Colors

namespace HairCarePlus.Client.Patient.Common.Views
{
    public class SkiaProgressRing : ContentView
    {
        public static readonly BindableProperty ProgressProperty =
            BindableProperty.Create(nameof(Progress), typeof(double), typeof(SkiaProgressRing), 0d, propertyChanged: OnProgressChanged);
        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public static readonly BindableProperty ThicknessProperty =
            BindableProperty.Create(nameof(Thickness), typeof(double), typeof(SkiaProgressRing), 4d, propertyChanged: (b,o,n) => ((SkiaProgressRing)b)._canvas.InvalidateSurface());
        public double Thickness
        {
            get => (double)GetValue(ThicknessProperty);
            set => SetValue(ThicknessProperty, value);
        }

        public event EventHandler? Completed;

        readonly SKCanvasView _canvas;
        double _animatedProgress;

        public SkiaProgressRing()
        {
            _canvas = new SKCanvasView { IgnorePixelScaling = false };
            _canvas.PaintSurface += OnPaintSurface;
            Content = _canvas;
            // Subscribe to theme changes to invalidate the canvas and redraw with new theme colors
            Application.Current.RequestedThemeChanged += (s, a) => _canvas.InvalidateSurface();
        }

        static void OnProgressChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is SkiaProgressRing ring && newValue is double target)
            {
                ring.AnimateProgressAsync(target);
            }
        }

        void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var info = e.Info;
            var surfaceCanvas = e.Surface.Canvas; // Renamed to avoid conflict
            surfaceCanvas.Clear();

            float cx = info.Width / 2f;
            float cy = info.Height / 2f;
            float thick = (float)Thickness;
            float radius = Math.Min(info.Width, info.Height) / 2f - thick / 2f;

            // Determine colors based on the current application theme
            bool isDarkTheme = Application.Current.RequestedTheme == AppTheme.Dark;
            
            SKColor trackColorResource;
            SKColor progressArcColorResource;

            if (isDarkTheme)
            {
                trackColorResource = (Application.Current.Resources["TaskCardBackgroundDark"] as SolidColorBrush)?.Color.ToSKColor() ?? SKColors.Gray;
                progressArcColorResource = SKColors.White;
            }
            else
            {
                trackColorResource = (Application.Current.Resources["TaskCardBackgroundLight"] as SolidColorBrush)?.Color.ToSKColor() ?? SKColors.LightGray;
                progressArcColorResource = SKColors.Black;
            }

            using var trackPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = thick,
                Color = trackColorResource, // Use theme-dependent color
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };
            surfaceCanvas.DrawCircle(cx, cy, radius, trackPaint);

            if (_animatedProgress > 0)
            {
                using var arcPaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = thick,
                    Color = progressArcColorResource, // Use theme-dependent color
                    IsAntialias = true,
                    StrokeCap = SKStrokeCap.Round
                };
                float sweep = 360f * (float)_animatedProgress;
                var rect = new SKRect(cx - radius, cy - radius, cx + radius, cy + radius);
                surfaceCanvas.DrawArc(rect, -90, sweep, false, arcPaint);

                // highlight dot
                // convert degrees to radians
                float endRad = (-90f + sweep) * ((float)Math.PI / 180f);
                float ex = cx + radius * (float)Math.Cos(endRad);
                float ey = cy + radius * (float)Math.Sin(endRad);
                using var dotPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = SKColors.White, IsAntialias = true };
                surfaceCanvas.DrawCircle(ex, ey, thick * 0.5f, dotPaint);
            }
        }

        async void AnimateProgressAsync(double target)
        {
            var from = _animatedProgress;
            var tcs = new TaskCompletionSource<bool>();
            var animation = new Animation(v =>
            {
                _animatedProgress = v;
                _canvas.InvalidateSurface();
            }, from, target, Easing.Linear);
            animation.Commit(this, "skiaRing", 16, 700, finished: (l, c) =>
            {
                if (target >= 1)
                    Completed?.Invoke(this, EventArgs.Empty);
                tcs.SetResult(true);
            });
            await tcs.Task;
        }
    }
} 