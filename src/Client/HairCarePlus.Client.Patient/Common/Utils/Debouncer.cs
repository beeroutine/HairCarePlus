using System;
using System.Threading;
using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Common.Utils
{
    /// <summary>
    /// Утилита для дебаунсинга операций - откладывает выполнение до завершения серии вызовов
    /// </summary>
    public class Debouncer
    {
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly object _lock = new();

        /// <summary>
        /// Откладывает выполнение операции на указанное время
        /// Если метод вызван повторно до истечения времени, предыдущий вызов отменяется
        /// </summary>
        public void Debounce(int milliseconds, Action action)
        {
            lock (_lock)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();
                var token = _cancellationTokenSource.Token;

                Task.Delay(milliseconds, token).ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                    {
                        action();
                    }
                }, TaskScheduler.Default);
            }
        }

        /// <summary>
        /// Асинхронная версия дебаунсинга
        /// </summary>
        public void Debounce(int milliseconds, Func<Task> asyncAction)
        {
            lock (_lock)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();
                var token = _cancellationTokenSource.Token;

                Task.Delay(milliseconds, token).ContinueWith(async t =>
                {
                    if (!t.IsCanceled)
                    {
                        await asyncAction();
                    }
                }, TaskScheduler.Default);
            }
        }

        /// <summary>
        /// Отменяет все ожидающие операции
        /// </summary>
        public void Cancel()
        {
            lock (_lock)
            {
                _cancellationTokenSource?.Cancel();
            }
        }

        /// <summary>
        /// Освобождает ресурсы
        /// </summary>
        public void Dispose()
        {
            Cancel();
            _cancellationTokenSource?.Dispose();
        }
    }
} 