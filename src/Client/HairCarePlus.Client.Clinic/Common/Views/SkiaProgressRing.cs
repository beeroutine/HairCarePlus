#pragma warning disable CS8602
#nullable enable
using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Core; // Provides AnimationExtensions
using Microsoft.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Microsoft.Maui.Graphics; // For Maui Colors

namespace HairCarePlus.Client.Clinic.Common.Views
{
    /// <summary>
    /// Skia-based animated progress ring, ported from Patient app to enable shared visuals.
    /// </summary>
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
            BindableProperty.Create(nameof(Thickness), typeof(double), typeof(SkiaProgressRing), 4d, propertyChanged: (b, o, n) => ((SkiaProgressRing)b)._canvas.InvalidateSurface());

        public double Thickness
        {
            get => (double)GetValue(ThicknessProperty);
            set => SetValue(ThicknessProperty, value);
        }

        public event EventHandler? Completed;

        private readonly SKCanvasView _canvas;
        private double _animatedProgress;
        private bool _isLoaded;
        private double? _pendingTarget;

        public SkiaProgressRing()
        {
            _canvas = new SKCanvasView { IgnorePixelScaling = false };
            _canvas.PaintSurface += OnPaintSurface;
            Content = _canvas;

            Loaded += (_, __) =>
            {
                _isLoaded = true;
                _canvas.InvalidateSurface();
                if (_pendingTarget is double pending)
                {
                    _pendingTarget = null;
                    AnimateProgressAsync(pending);
                }
            };

            Unloaded += (_, __) => _isLoaded = false;
            Application.Current.RequestedThemeChanged += (_, __) => _canvas.InvalidateSurface();
        }

        private static void OnProgressChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is SkiaProgressRing ring && newValue is double target)
            {
                ring.AnimateProgressAsync(target);
            }
        }

        private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface?.Canvas;
            var info = e.Info;
            canvas?.Clear();

            // Guard against invalid sizes or NaN values to avoid CoreGraphics NaN warnings
            if (info.Width <= 0 || info.Height <= 0) return;
            if (double.IsNaN(Thickness) || double.IsInfinity(Thickness)) return;
            if (double.IsNaN(_animatedProgress) || double.IsInfinity(_animatedProgress)) return;

            float cx = info.Width / 2f;
            float cy = info.Height / 2f;
            float thick = (float)Thickness;
            float radius = Math.Min(info.Width, info.Height) / 2f - thick / 2f;
            if (radius <= 0) return;

            bool isDarkTheme = Application.Current?.RequestedTheme == AppTheme.Dark;

            SKColor trackColor;
            if (isDarkTheme)
            {
                trackColor = (Application.Current?.Resources["TaskCardBackgroundDark"] as SolidColorBrush)?.Color.ToSKColor() ?? SKColors.Gray;
            }
            else
            {
                trackColor = (Application.Current?.Resources["TaskCardBackgroundLight"] as SolidColorBrush)?.Color.ToSKColor() ?? SKColors.LightGray;
            }

            SKColor progressColor = isDarkTheme ? SKColors.White : SKColors.Black;

            using var trackPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = thick,
                Color = trackColor,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };
            canvas.DrawCircle(cx, cy, radius, trackPaint);

            if (_animatedProgress > 0)
            {
                using var arcPaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = thick,
                    Color = progressColor,
                    IsAntialias = true,
                    StrokeCap = SKStrokeCap.Round
                };
                float sweep = 360f * (float)Math.Clamp(_animatedProgress, 0d, 1d);
                var rect = new SKRect(cx - radius, cy - radius, cx + radius, cy + radius);
                canvas.DrawArc(rect, -90, sweep, false, arcPaint);

                // Draw leading dot
                float endRad = (-90f + sweep) * ((float)Math.PI / 180f);
                float ex = cx + radius * (float)Math.Cos(endRad);
                float ey = cy + radius * (float)Math.Sin(endRad);
                using var dotPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = progressColor, IsAntialias = true };
                canvas.DrawCircle(ex, ey, thick * 0.5f, dotPaint);
            }
        }

        private async void AnimateProgressAsync(double target)
        {
            if (!_isLoaded || Handler == null)
            {
                _pendingTarget = target;
                return;
            }

            double from = _animatedProgress;
            var tcs = new TaskCompletionSource<bool>();

            this.AbortAnimation("skiaRing");

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