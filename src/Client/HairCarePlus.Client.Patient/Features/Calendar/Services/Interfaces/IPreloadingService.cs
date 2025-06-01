using System;
using System.Threading;
using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces
{
    /// <summary>
    /// Сервис для предзагрузки данных календаря
    /// </summary>
    public interface IPreloadingService
    {
        /// <summary>
        /// Предзагружает события для соседних дат
        /// </summary>
        Task PreloadAdjacentDatesAsync(DateTime centerDate, int daysBefore = 3, int daysAfter = 3, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Предзагружает события для диапазона дат
        /// </summary>
        Task PreloadDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Начинает фоновую предзагрузку на основе паттернов использования
        /// </summary>
        Task StartBackgroundPreloadingAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Останавливает фоновую предзагрузку
        /// </summary>
        void StopBackgroundPreloading();
        
        /// <summary>
        /// Очищает очередь предзагрузки
        /// </summary>
        void ClearPreloadQueue();
    }
} 