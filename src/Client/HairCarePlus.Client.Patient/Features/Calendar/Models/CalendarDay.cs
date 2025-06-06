using System;
using Microsoft.Maui.Graphics;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HairCarePlus.Client.Patient.Features.Calendar.Models
{
    /// <summary>
    /// Представляет один день в горизонтальном календаре вместе с пред-вычисленными
    /// данными (есть ли события, цвет индикатора, выбран ли день). Это позволяет
    /// отказаться от тяжёлых IValueConverter на уровне XAML.
    /// </summary>
    public class CalendarDay : ReactiveObject
    {
        public DateTime Date { get; }

        [Reactive]
        public bool HasEvents { get; set; }

        [Reactive]
        public Color IndicatorColor { get; set; } = Colors.Transparent;

        [Reactive]
        public bool IsSelected { get; set; }

        public CalendarDay(DateTime date)
        {
            Date = date.Date;
        }
    }
} 