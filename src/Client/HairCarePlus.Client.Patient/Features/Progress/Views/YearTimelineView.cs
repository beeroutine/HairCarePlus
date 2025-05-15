using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;
using System;

namespace HairCarePlus.Client.Patient.Features.Progress.Views;

/// <summary>
/// Простая полоса 0-12 мес с маркером сегодняшнего дня.
/// Отрисовывает 0-12 месяцев с маркером сегодняшнего дня и подписью сегментов 0-3-6-9-12 мес.
/// </summary>
public sealed class YearTimelineView : GraphicsView, IDrawable
{
    public YearTimelineView()
    {
        // Дополнительное пространство под подписи (12 px текста + отступ).
        HeightRequest = 32;
        Drawable = this;
    }

    #region BindableProperty
    public static readonly BindableProperty SurgeryDateProperty = BindableProperty.Create(
        nameof(SurgeryDate), typeof(DateTime), typeof(YearTimelineView), DateTime.Today, propertyChanged: (b, o, n) => ((YearTimelineView)b).Invalidate());

    public DateTime SurgeryDate
    {
        get => (DateTime)GetValue(SurgeryDateProperty);
        set => SetValue(SurgeryDateProperty, value);
    }
    #endregion

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        const float labelFontSize = 10f;
        const float labelSpacing = 2f;
        const float barHeight = 8f;

        // Полоска располагается сразу под подписями.
        var barY = labelFontSize + labelSpacing;
        var barRect = new RectF((float)dirtyRect.Left, barY, (float)dirtyRect.Width, barHeight);

        // Use theme-aware track color
        var isDark = Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark;
        Color trackColor = GetColor(isDark ? "ProgressTrackDark" : "ProgressTrackLight", isDark ? Colors.Gray : Colors.LightGray);
        canvas.FillColor = trackColor;
        canvas.FillRoundedRectangle(barRect, barHeight / 2);

        // Progress bar (primary)
        var progress = (float)Math.Clamp((DateTime.Today - SurgeryDate).TotalDays / 365f, 0, 1);
        if (progress > 0)
        {
            var progressWidth = barRect.Width * progress;
            var progressRect = new RectF(barRect.Left, barRect.Top, progressWidth, barHeight);
            Color primaryColor = Colors.Blue;
            if (Microsoft.Maui.Controls.Application.Current?.Resources.TryGetValue("Primary", out var obj) == true && obj is Color col)
                primaryColor = col;

            // Slight gradient effect – darker to lighter left→right for better visual depth
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

        // Marker (circle)
        var markerX = barRect.Left + barRect.Width * progress;
        var radius = barHeight * 1.2f;
        canvas.FillColor = Colors.White;
        canvas.FillCircle(markerX, barRect.Top + barHeight / 2, radius / 2);
        canvas.StrokeColor = Colors.Black.WithAlpha(0.3f);
        canvas.StrokeSize = 1;
        canvas.DrawCircle(markerX, barRect.Top + barHeight / 2, radius / 2);

        // Segment labels (0-3-6-9-12)
        string[] labels = { "0", "3", "6", "9", "12" };
        canvas.FontSize = labelFontSize;
        // Theme-aware label color
        canvas.FontColor = GetColor(isDark ? "Gray400" : "Gray500", Colors.Gray);

        for (int i = 0; i < labels.Length; i++)
        {
            float t = i / (float)(labels.Length - 1); // 0 → 1
            float x = barRect.Left + barRect.Width * t;
            canvas.DrawString(labels[i], x, 0, HorizontalAlignment.Center);
        }

        // Segment tick marks (1m,3m,6m,12m)
        var daysPerMonth = 30f;
        float[] monthBoundaries = { 1, 3, 6 }; // skip 12 (end)
        canvas.StrokeColor = GetColor(isDark ? "Gray500" : "Gray300", Colors.DarkGray);
        canvas.StrokeSize = 1;
        foreach (var m in monthBoundaries)
        {
            float t = (m * daysPerMonth) / 365f;
            float x = barRect.Left + barRect.Width * t;
            canvas.DrawLine(x, barRect.Top, x, barRect.Top + barHeight);
        }
    }

    private static Color GetColor(string key, Color fallback)
    {
        if (Microsoft.Maui.Controls.Application.Current?.Resources.TryGetValue(key, out var obj) == true && obj is Color col)
            return col;
        return fallback;
    }
} 