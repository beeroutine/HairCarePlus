using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace HairCarePlus.Client.Patient.Common.Views
{
    class RingDrawable : IDrawable
    {
        readonly ProgressRing _owner;
        public RingDrawable(ProgressRing owner) => _owner = owner;

        public void Draw(ICanvas canvas, RectF rect)
        {
            canvas.Antialias = true;

            float progress = (float)Math.Clamp(_owner.Progress, 0, 1);
            float thick = (float)_owner.Thickness;
            
            // Apply pulse effect when near completion
            if (_owner.IsPulsing)
            {
                float pulsePhase = (float)(DateTime.Now.Millisecond / 1000.0);
                float pulseFactor = 1f + (MathF.Sin(pulsePhase * MathF.PI * 2) * 0.05f);
                thick *= pulseFactor;
            }
            
            float cx = rect.Center.X;
            float cy = rect.Center.Y;
            float radius = Math.Min(rect.Width, rect.Height) / 2f - thick / 2f;

            // Semi-transparent track — fallback на серый если Application/ресурсы недоступны
            Color trackColor;
            var app = Application.Current;
            if (app?.Resources is not ResourceDictionary res)
            {
                trackColor = Colors.Gray; // safe default
            }
            else
            {
                var dark = app.RequestedTheme == AppTheme.Dark;
                var key = dark ? "ProgressTrackDark" : "ProgressTrackLight";
                trackColor = res.TryGetValue(key, out var obj) && obj is Color c ? c : Colors.Gray;
            }
            canvas.StrokeColor = trackColor.WithAlpha(0.5f);
            canvas.StrokeSize = thick;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.DrawCircle(cx, cy, radius);

            // Draw progress arc – clockwise from 12 o'clock
            if (progress > 0)
            {
                float startAngle = -90f; // 12 o'clock
                float sweepAngle = 360f * progress; // calculate sweep angle
                float endAngle = startAngle + sweepAngle; // calculate end angle for dot

                // Main progress arc - clockwise direction
                canvas.StrokeColor = _owner.RingColor;
                canvas.StrokeSize = thick;
                canvas.StrokeLineCap = LineCap.Round;
                canvas.DrawArc(cx - radius, cy - radius, radius * 2, radius * 2,
                               startAngle, sweepAngle, false, true); // draw stroke only, clockwise

                // Highlight dot at arc end (clockwise calculation)
                float endAngleRad = endAngle * MathF.PI / 180f; // dot at endAngle known
                float endX = cx + radius * MathF.Cos(endAngleRad);
                float endY = cy + radius * MathF.Sin(endAngleRad);

                // Outer halo
                canvas.FillColor = Colors.White.WithAlpha(0.35f);
                canvas.FillCircle(endX, endY, thick * 0.8f);

                // Core dot
                canvas.FillColor = Colors.White.WithAlpha(0.9f);
                canvas.FillCircle(endX, endY, thick * 0.4f);
            }
        }
    }
} 