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
            // Минималистичный дизайн с тонкими линиями
            const float labelFontSize = 10f;
            const float labelSpacing = 3f;
            const float barHeight = 1f; // Тоньше
            const float markerSize = 4f; // Меньший маркер

            var barY = 0f;
            var barRect = new RectF(dirtyRect.Left, barY, dirtyRect.Width, barHeight);

            var isDark = Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark;
            
            // Тонкая линия трека
            Color trackColor = GetColor(isDark ? "Gray300" : "Gray200", Colors.LightGray);
            canvas.StrokeColor = trackColor;
            canvas.StrokeSize = barHeight;
            canvas.DrawLine(barRect.Left, barRect.Top + barHeight/2, barRect.Right, barRect.Top + barHeight/2);

            var progress = (float)Math.Clamp((DateTime.Today - surgeryDate).TotalDays / 365f, 0, 1);
            if (progress > 0)
            {
                // Линия прогресса - акцентный цвет
                var progressEnd = barRect.Left + barRect.Width * progress;
                Color primaryColor = GetColor("Primary", Colors.Blue);
                canvas.StrokeColor = primaryColor;
                canvas.StrokeSize = barHeight * 2; // Чуть толще для контраста
                canvas.DrawLine(barRect.Left, barRect.Top + barHeight/2, progressEnd, barRect.Top + barHeight/2);
            }

            // Минималистичный маркер
            var markerX = barRect.Left + barRect.Width * progress;
            Color markerColor = GetColor("Primary", Colors.Blue);
            canvas.FillColor = markerColor;
            canvas.FillCircle(markerX, barRect.Top + barHeight / 2, markerSize);

            // Упрощенные метки месяцев
            float[] months = { 0f, 3f, 6f, 9f, 12f };
            canvas.FontSize = labelFontSize;
            canvas.FontColor = GetColor(isDark ? "Gray500" : "Gray600", Colors.Gray);
            
            foreach (var m in months)
            {
                float t = (m * 30f) / 365f; // Приблизительно
                float x = barRect.Left + barRect.Width * t;
                float labelY = barRect.Bottom + labelSpacing + 8;
                
                // Маленькие вертикальные метки
                canvas.StrokeColor = GetColor(isDark ? "Gray400" : "Gray300", Colors.LightGray);
                canvas.StrokeSize = 0.5f;
                canvas.DrawLine(x, barRect.Top - 2, x, barRect.Top + 3);
                
                // Метки текста
                canvas.DrawString($"{(int)m}", x, labelY, HorizontalAlignment.Center);
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