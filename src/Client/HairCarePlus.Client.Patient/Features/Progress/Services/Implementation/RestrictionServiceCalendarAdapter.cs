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
                        IconType = RestrictionIconMapper.FromTitle(e.Title),
                        DaysRemaining = Math.Max(1, (e.EndDate!.Value.Date - today.Date).Days + 1),
                        DetailedDescription = string.IsNullOrWhiteSpace(e.Description) ? GetDetailedDescription(e.Title) : e.Description
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
                        IconType = RestrictionIconMapper.FromTitle(r.Title),
                        DaysRemaining = Math.Max(1, (surgeryDate.AddDays(r.EndDay - 1) - today).Days + 1),
                        DetailedDescription = r.Description
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

    private static string GetDetailedDescription(string title)
    {
        var lowerTitle = title.ToLowerInvariant();
        
        return lowerTitle switch
        {
            var t when t.Contains("курен") || t.Contains("сигарет") || t.Contains("табак") => 
                "Курение замедляет заживление тканей и ухудшает кровообращение в области трансплантации. Никотин сужает кровеносные сосуды, что снижает поступление кислорода и питательных веществ к волосяным луковицам.",
            
            var t when t.Contains("алкоголь") || t.Contains("пиво") || t.Contains("вино") || t.Contains("водка") => 
                "Алкоголь обезвоживает организм и ослабляет иммунную систему, что может привести к воспалению и замедлить процесс заживления. Также алкоголь может взаимодействовать с назначенными лекарствами.",
            
            var t when t.Contains("спорт") || t.Contains("физическ") || t.Contains("нагрузк") || t.Contains("тренировк") => 
                "Интенсивные физические нагрузки повышают кровяное давление и могут вызвать кровотечение в области трансплантации. Потоотделение также может привести к инфекции.",
            
            var t when t.Contains("солнц") || t.Contains("загар") || t.Contains("пляж") || t.Contains("солярий") => 
                "Прямые солнечные лучи могут повредить нежную кожу головы и привести к образованию рубцов. УФ-излучение также может вызвать воспаление и замедлить заживление.",
            
            var t when t.Contains("мыт") || t.Contains("голов") || t.Contains("шампун") => 
                "В первые дни после операции кожа головы особенно чувствительна. Мытье может смыть защитные корочки и повредить новые волосяные луковицы.",
            
            var t when t.Contains("басс") || t.Contains("плаван") || t.Contains("вода") || t.Contains("душ") => 
                "Погружение головы в воду может привести к инфекции и размягчению корочек, которые защищают область трансплантации. Хлорированная вода особенно вредна.",
            
            var t when t.Contains("стрижк") || t.Contains("машинк") || t.Contains("брить") => 
                "Механическое воздействие на кожу головы может повредить новые волосяные луковицы и нарушить процесс приживления трансплантатов.",
            
            var t when t.Contains("шляп") || t.Contains("шапк") || t.Contains("кепк") => 
                "Тесные головные уборы могут нарушить кровообращение и создать давление на область трансплантации, что препятствует нормальному заживлению.",
            
            var t when t.Contains("наклон") || t.Contains("голов") || t.Contains("нагиб") => 
                "Наклоны головы увеличивают приток крови к области операции, что может вызвать отеки и кровотечение. Держите голову приподнятой.",
            
            var t when t.Contains("лежать") || t.Contains("спать") || t.Contains("сон") => 
                "Лежание на животе или боку может создать давление на область трансплантации и повредить новые волосяные луковицы. Спите на спине с приподнятой головой.",
            
            var t when t.Contains("секс") || t.Contains("интим") || t.Contains("близост") => 
                "Повышенная физическая активность и учащенное сердцебиение могут привести к кровотечению и отекам в области операции.",
            
            _ => "Соблюдение этого ограничения важно для успешного заживления и приживления трансплантированных волос. Пожалуйста, следуйте рекомендациям вашего врача."
        };
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
        public string? Description { get; set; }
    }
} 