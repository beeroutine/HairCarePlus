using System;
using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Common.Services
{
    /// <summary>
    /// Менеджер для управления конфетти анимациями
    /// </summary>
    public interface IConfettiManager
    {
        /// <summary>
        /// Показывает конфетти анимацию
        /// </summary>
        /// <param name="duration">Длительность анимации в миллисекундах</param>
        /// <param name="particleCount">Количество частиц</param>
        Task ShowConfettiAsync(int duration = 3000, int particleCount = 100);
        
        /// <summary>
        /// Останавливает текущую анимацию
        /// </summary>
        void StopConfetti();
        
        /// <summary>
        /// Проверяет, активна ли анимация
        /// </summary>
        bool IsAnimating { get; }
        
        /// <summary>
        /// Настройки производительности
        /// </summary>
        void ConfigurePerformance(ConfettiPerformanceLevel level);
    }
    
    /// <summary>
    /// Уровни производительности для конфетти
    /// </summary>
    public enum ConfettiPerformanceLevel
    {
        /// <summary>
        /// Минимальное количество частиц, простая анимация
        /// </summary>
        Low,
        
        /// <summary>
        /// Среднее количество частиц, стандартная анимация
        /// </summary>
        Medium,
        
        /// <summary>
        /// Максимальное количество частиц, полная анимация
        /// </summary>
        High
    }
} 