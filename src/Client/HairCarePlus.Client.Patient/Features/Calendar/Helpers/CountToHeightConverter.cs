using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Calendar.Helpers
{
    /// <summary>
    /// Конвертер для преобразования количества элементов в высоту контейнера
    /// </summary>
    public class CountToHeightConverter : IValueConverter
    {
        /// <summary>
        /// Минимальная высота элемента
        /// </summary>
        public double MinimumHeight { get; set; } = 100;
        
        /// <summary>
        /// Высота одного элемента
        /// </summary>
        public double ItemHeight { get; set; } = 90;
        
        /// <summary>
        /// Максимальное количество отображаемых элементов
        /// </summary>
        public int MaxVisibleItems { get; set; } = 4;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                // Если нет элементов, возвращаем минимальную высоту
                if (count == 0)
                    return MinimumHeight;
                
                // Ограничиваем количество элементов для отображения
                var visibleCount = Math.Min(count, MaxVisibleItems);
                
                // Рассчитываем высоту в зависимости от количества элементов
                return visibleCount * ItemHeight;
            }
            
            return MinimumHeight;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 