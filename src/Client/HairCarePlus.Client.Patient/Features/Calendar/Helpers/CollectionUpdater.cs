using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HairCarePlus.Client.Patient.Features.Calendar.Helpers
{
    /// <summary>
    /// Утилита для эффективного обновления ObservableCollection без пересоздания
    /// </summary>
    public static class CollectionUpdater
    {
        /// <summary>
        /// Обновляет коллекцию, добавляя новые элементы и удаляя отсутствующие
        /// </summary>
        public static void UpdateCollection<T>(
            ObservableCollection<T> target,
            IEnumerable<T> source,
            Func<T, T, bool> comparer)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));

            var sourceList = source.ToList();

            // Удаляем элементы, которых нет в source
            for (int i = target.Count - 1; i >= 0; i--)
            {
                if (!sourceList.Any(s => comparer(s, target[i])))
                {
                    target.RemoveAt(i);
                }
            }

            // Добавляем новые элементы
            foreach (var item in sourceList)
            {
                if (!target.Any(t => comparer(item, t)))
                {
                    target.Add(item);
                }
            }
        }

        /// <summary>
        /// Обновляет коллекцию с сортировкой
        /// </summary>
        public static void UpdateCollectionWithSort<T, TKey>(
            ObservableCollection<T> target,
            IEnumerable<T> source,
            Func<T, T, bool> comparer,
            Func<T, TKey> sortKeySelector)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));
            if (sortKeySelector == null) throw new ArgumentNullException(nameof(sortKeySelector));

            // Сначала обновляем коллекцию
            UpdateCollection(target, source, comparer);

            // Затем сортируем
            var sorted = target.OrderBy(sortKeySelector).ToList();
            
            // Перемещаем элементы на правильные позиции
            for (int i = 0; i < sorted.Count; i++)
            {
                var item = sorted[i];
                var currentIndex = target.IndexOf(item);
                if (currentIndex != i)
                {
                    target.Move(currentIndex, i);
                }
            }
        }

        /// <summary>
        /// Обновляет коллекцию с учетом производительности (батчинг)
        /// </summary>
        public static void BatchUpdateCollection<T>(
            ObservableCollection<T> target,
            IEnumerable<T> source,
            Func<T, T, bool> comparer,
            int batchSize = 50)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));

            var sourceList = source.ToList();
            var toRemove = new List<T>();
            var toAdd = new List<T>();

            // Находим элементы для удаления
            foreach (var item in target)
            {
                if (!sourceList.Any(s => comparer(s, item)))
                {
                    toRemove.Add(item);
                }
            }

            // Находим элементы для добавления
            foreach (var item in sourceList)
            {
                if (!target.Any(t => comparer(item, t)))
                {
                    toAdd.Add(item);
                }
            }

            // Удаляем батчами
            foreach (var batch in toRemove.Chunk(batchSize))
            {
                foreach (var item in batch)
                {
                    target.Remove(item);
                }
            }

            // Добавляем батчами
            foreach (var batch in toAdd.Chunk(batchSize))
            {
                foreach (var item in batch)
                {
                    target.Add(item);
                }
            }
        }
    }
} 