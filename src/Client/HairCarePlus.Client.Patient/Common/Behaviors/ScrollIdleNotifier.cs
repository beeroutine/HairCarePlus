namespace HairCarePlus.Client.Patient.Common.Behaviors
{
    /// <summary>
    /// Кросс-платформенный помощник: позволяет узнать, когда <see cref="CollectionView"/> завершила инерционную прокрутку.
    /// Реализации находятся в Platform-specific partial файлах.
    /// </summary>
    public static partial class ScrollIdleNotifier
    {
        /// <summary>
        /// Подключается к нативному контролу <see cref="CollectionView"/> и вызывает callback, когда прокрутка переходит в состояние Idle.
        /// </summary>
        /// <param name="collectionView">Коллекция, к которой надо привязаться.</param>
        /// <param name="onIdle">Действие, вызываемое на UI-потоке.</param>
        static partial void AttachInternal(Microsoft.Maui.Controls.CollectionView collectionView, System.Action onIdle);

        /// <summary>
        /// Публичный удобный метод-обёртка, вызывающий platform-specific реализацию.
        /// </summary>
        /// <param name="collectionView">Целевая CollectionView.</param>
        /// <param name="onIdle">Колбэк при завершении прокрутки.</param>
        public static void Attach(Microsoft.Maui.Controls.CollectionView collectionView, System.Action onIdle)
        {
            AttachInternal(collectionView, onIdle);
        }
    }
} 