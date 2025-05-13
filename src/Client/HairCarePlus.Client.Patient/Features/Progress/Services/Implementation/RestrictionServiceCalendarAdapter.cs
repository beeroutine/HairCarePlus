using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;
using HairCarePlus.Client.Patient.Features.Progress.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Client.Patient.Features.Progress.Services.Implementation;

/// <summary>
/// Адаптер, который берёт ограничения из календарной БД (ICalendarService) и
/// преобразует их в модель <see cref="RestrictionTimer"/> для Progress-модуля.
/// Тем самым устраняем дублирование с JSON-чтением.
/// </summary>
public sealed class RestrictionServiceCalendarAdapter : IRestrictionService
{
    private readonly ICalendarService _calendar;
    private readonly ILogger<RestrictionServiceCalendarAdapter> _logger;

    public RestrictionServiceCalendarAdapter(ICalendarService calendar, ILogger<RestrictionServiceCalendarAdapter> logger)
    {
        _calendar = calendar;
        _logger = logger;
    }

    public Task<IReadOnlyList<RestrictionTimer>> GetActiveRestrictionsAsync(CancellationToken cancellationToken = default)
        => GetRestrictionsForDateAsync(DateOnly.FromDateTime(DateTime.Today), cancellationToken);

    public async Task<IReadOnlyList<RestrictionTimer>> GetRestrictionsForDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        try
        {
            var today = date.ToDateTime(TimeOnly.MinValue);
            var events = await _calendar.GetActiveRestrictionsAsync();

            var timers = events
                .Where(e => e.EndDate.HasValue && e.EndDate.Value.Date >= today)
                .Select(e => new RestrictionTimer
                {
                    Title = e.Title,
                    DaysRemaining = Math.Max(1, (e.EndDate!.Value.Date - today.Date).Days + 1)
                })
                .OrderByDescending(t => t.DaysRemaining)
                .ToList();

            return timers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load restrictions from calendar service");
            return new List<RestrictionTimer>();
        }
    }
} 