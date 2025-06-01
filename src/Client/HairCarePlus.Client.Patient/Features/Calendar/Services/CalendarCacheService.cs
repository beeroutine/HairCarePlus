using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Models;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services
{
    /// <summary>
    /// Thread‑safe in‑memory кэш событий календаря на клиенте.
    /// </summary>
    public interface ICalendarCacheService
    {
        /// <summary>
        /// Пытается получить события для указанной даты.
        /// </summary>
        bool TryGet(DateTime date, out List<CalendarEvent> events, out DateTimeOffset lastUpdate);

        /// <summary>
        /// Сохраняет события и обновляет метку времени.
        /// </summary>
        void Set(DateTime date, IEnumerable<CalendarEvent> events);

        /// <summary>
        /// Проверяет, является ли запись свежей (обновлена менее, чем <paramref name="freshFor" />).
        /// </summary>
        bool IsFresh(DateTime date, TimeSpan freshFor);

        /// <summary>
        /// Удаляет записи старше указанного количества дней от <see cref="DateTime.Today"/>.
        /// </summary>
        void CleanupOldEntries(int keepDays = 30);
    }

    /// <inheritdoc />
    public sealed class CalendarCacheService : ICalendarCacheService
    {
        private readonly Dictionary<DateTime, List<CalendarEvent>> _cache = new();
        private readonly Dictionary<DateTime, DateTimeOffset> _updateTimes = new();
        private readonly object _lock = new();

        public bool TryGet(DateTime date, out List<CalendarEvent> events, out DateTimeOffset lastUpdate)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(date.Date, out events) && _updateTimes.TryGetValue(date.Date, out lastUpdate))
                    return true;
                events = new List<CalendarEvent>();
                lastUpdate = default;
                return false;
            }
        }

        public void Set(DateTime date, IEnumerable<CalendarEvent> events)
        {
            if (events == null) return;
            lock (_lock)
            {
                _cache[date.Date] = events.ToList();
                _updateTimes[date.Date] = DateTimeOffset.Now;
            }
        }

        public bool IsFresh(DateTime date, TimeSpan freshFor)
        {
            lock (_lock)
            {
                return _updateTimes.TryGetValue(date.Date, out var ts) && (DateTimeOffset.Now - ts) <= freshFor;
            }
        }

        public void CleanupOldEntries(int keepDays = 30)
        {
            var threshold = DateTime.Today.AddDays(-keepDays);
            List<DateTime> toRemove;
            lock (_lock)
            {
                toRemove = _cache.Keys.Where(d => d < threshold).ToList();
                foreach (var k in toRemove)
                {
                    _cache.Remove(k);
                    _updateTimes.Remove(k);
                }
            }
        }
    }
} 