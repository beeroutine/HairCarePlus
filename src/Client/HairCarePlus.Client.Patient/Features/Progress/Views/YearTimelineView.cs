using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;
using System;
using Microsoft.Maui.ApplicationModel;
using HairCarePlus.Client.Patient.Features.Progress.Views;
using HairCarePlus.Client.Patient.Common.Behaviors;
using HairCarePlus.Client.Patient.Features.Progress.Services.Interfaces;

namespace HairCarePlus.Client.Patient.Features.Progress.Views;

/// <summary>
/// Простая полоса 0-12 мес с маркером сегодняшнего дня.
/// Отрисовывает 0-12 месяцев с маркером сегодняшнего дня и подписью сегментов 0-3-6-9-12 мес.
/// </summary>
public sealed class YearTimelineView : GraphicsView, IDrawable, IYearTimelineView
{
    private readonly ITimelineService _timelineService;

    public YearTimelineView()
    {
        // Дополнительное пространство под подписи (12 px текста + отступ).
        HeightRequest = 32;
        Drawable = this;
        // Resolve timeline service
        _timelineService = ServiceHelper.GetService<ITimelineService>();
        if (_timelineService == null)
            throw new InvalidOperationException("TimelineService is not available");
        // Tap on the timeline to show day info
        var tap = new Microsoft.Maui.Controls.TapGestureRecognizer();
        tap.Command = new Microsoft.Maui.Controls.Command(OnMarkerTapped);
        GestureRecognizers.Add(tap);
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
        // Delegate drawing to TimelineService
        _timelineService.DrawTimeline(canvas, dirtyRect, SurgeryDate);
    }

    private void OnMarkerTapped()
    {
        // Get marker info
        var message = _timelineService.GetMarkerMessage(SurgeryDate);
        // Show popup on UI thread
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var page = Microsoft.Maui.Controls.Application.Current?.MainPage;
            if (page != null)
                await page.DisplayAlert("Информация", message, "OK");
        });
    }

    private static Color GetColor(string key, Color fallback)
    {
        // remain for fallback if needed
        if (Microsoft.Maui.Controls.Application.Current?.Resources.TryGetValue(key, out var obj) == true && obj is Color col)
            return col;
        return fallback;
    }
} 