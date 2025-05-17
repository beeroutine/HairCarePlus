using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;
using HairCarePlus.Client.Patient.Features.Progress.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using System.IO;
using System.Text.Json;
using HairCarePlus.Client.Patient.Infrastructure.Services.Interfaces;

namespace HairCarePlus.Client.Patient.Features.Progress.Services.Implementation;

/// <summary>
/// Адаптер, который берёт ограничения из календарной БД (ICalendarService) и
/// преобразует их в модель <see cref="RestrictionTimer"/> для Progress-модуля.
/// Тем самым устраняем дублирование с JSON-чтением.
/// </summary>
public sealed class RestrictionServiceCalendarAdapter : IRestrictionService
{
    private readonly ICalendarService _calendar;
    private readonly IProfileService _profile;
    private readonly ILogger<RestrictionServiceCalendarAdapter> _logger;

    public RestrictionServiceCalendarAdapter(ICalendarService calendar, IProfileService profileService, ILogger<RestrictionServiceCalendarAdapter> logger)
    {
        _calendar = calendar;
        _profile = profileService;
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

            // Если календарь уже содержит ограничения — используем их.
            if (events?.Any() == true)
            {
                return events
                    .Where(e => e.EndDate.HasValue && e.EndDate.Value.Date >= today)
                    .Select(e => new RestrictionTimer
                    {
                        Title = e.Title,
                        DaysRemaining = Math.Max(1, (e.EndDate!.Value.Date - today.Date).Days + 1)
                    })
                    .OrderByDescending(t => t.DaysRemaining)
                    .ToList();
            }

            // Fallback: берём JSON-расписание и считаем дни относительно даты операции (ProfileService)
            try
            {
                var schedule = await LoadFallbackRestrictionScheduleAsync();

                var surgeryDate = _profile.SurgeryDate;

                var list = schedule
                    .Where(r => surgeryDate.AddDays(r.EndDay - 1) >= today)
                    .Select(r => new RestrictionTimer
                    {
                        Title = r.Title,
                        DaysRemaining = Math.Max(1, (surgeryDate.AddDays(r.EndDay - 1) - today).Days + 1)
                    })
                    .OrderBy(t => t.DaysRemaining)
                    .ToList();

                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load fallback restriction schedule");
            }

            return new List<RestrictionTimer>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load restrictions from calendar service");
            return new List<RestrictionTimer>();
        }
    }

    private static async Task<List<FallbackRestriction>> LoadFallbackRestrictionScheduleAsync()
    {
        const string FileName = "RestrictionSchedule.json";
        await using var stream = await FileSystem.OpenAppPackageFileAsync(FileName);
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        var list = JsonSerializer.Deserialize<List<FallbackRestriction>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return list ?? new List<FallbackRestriction>();
    }

    private sealed class FallbackRestriction
    {
        public string Title { get; set; } = string.Empty;
        public int StartDay { get; set; }
        public int EndDay { get; set; }
        public int DurationDays { get; set; }
    }
} 