using System;
using Microsoft.Maui.Graphics;
using HairCarePlus.Client.Patient.Features.Progress.Services.Interfaces;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Progress.Services.Implementation
{
    public class TimelineService : ITimelineService
    {
        public void DrawTimeline(ICanvas canvas, RectF dirtyRect, DateTime surgeryDate)
        {
            // Original drawing logic from YearTimelineView.Draw
            const float labelFontSize = 12f;
            const float labelSpacing = 4f;
            const float barHeight = 3f;

            var barY = 0f;
            var barRect = new RectF(dirtyRect.Left, barY, dirtyRect.Width, barHeight);

            var isDark = Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark;
            Color trackColor = GetColor(isDark ? "ProgressTrackDark" : "ProgressTrackLight", isDark ? Colors.Gray : Colors.LightGray);
            canvas.FillColor = trackColor;
            canvas.FillRoundedRectangle(barRect, barHeight / 2);

            var progress = (float)Math.Clamp((DateTime.Today - surgeryDate).TotalDays / 365f, 0, 1);
            if (progress > 0)
            {
                var progressWidth = barRect.Width * progress;
                var progressRect = new RectF(barRect.Left, barRect.Top, progressWidth, barHeight);
                Color primaryColor = Colors.Blue;
                if (Microsoft.Maui.Controls.Application.Current?.Resources.TryGetValue("Primary", out var obj) == true && obj is Color col)
                    primaryColor = col;

                canvas.SaveState();
                var gradient = new LinearGradientPaint
                {
                    StartPoint = new PointF(progressRect.Left, 0),
                    EndPoint = new PointF(progressRect.Right, 0),
                    StartColor = primaryColor,
                    EndColor = primaryColor.WithAlpha(0.7f)
                };
                canvas.SetFillPaint(gradient, progressRect);
                canvas.FillRoundedRectangle(progressRect, barHeight / 2);
                canvas.RestoreState();
            }

            var markerX = barRect.Left + barRect.Width * progress;
            var radius = barHeight * 1.2f;
            canvas.FillColor = Colors.White;
            canvas.FillCircle(markerX, barRect.Top + barHeight / 2, radius / 2);
            canvas.StrokeColor = Colors.Black.WithAlpha(0.3f);
            canvas.StrokeSize = 1;
            canvas.DrawCircle(markerX, barRect.Top + barHeight / 2, radius / 2);

            float daysPerMonth = 30f;
            float[] months = { 1f, 3f, 6f, 12f };
            canvas.StrokeColor = GetColor(isDark ? "Gray500" : "Gray300", Colors.DarkGray);
            canvas.StrokeSize = 1;
            canvas.FontSize = labelFontSize;
            canvas.FontColor = GetColor(isDark ? "Gray400" : "Gray500", Colors.Gray);
            foreach (var m in months)
            {
                float t = (m * daysPerMonth) / 365f;
                float x = barRect.Left + barRect.Width * t;
                canvas.DrawLine(x, barRect.Top, x, barRect.Top + barHeight);
                float labelY = barRect.Bottom + labelSpacing;
                canvas.DrawString($"{m}m", x, labelY, HorizontalAlignment.Center);
            }
        }

        public string GetMarkerMessage(DateTime surgeryDate, int totalDays = 365)
        {
            var days = (DateTime.Today - surgeryDate).Days;
            if (days < 0) days = 0;
            return $"День {days} из {totalDays}";
        }

        private static Color GetColor(string key, Color fallback)
        {
            if (Microsoft.Maui.Controls.Application.Current?.Resources.TryGetValue(key, out var obj) == true && obj is Color col)
                return col;
            return fallback;
        }
    }
} 