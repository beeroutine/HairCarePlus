using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Domain.Entities;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Models;
using HairCarePlus.Client.Patient.Infrastructure.Storage;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services;

public class EventGeneratorService : IEventGeneratorService
{
    private readonly ILocalStorageService _localStorage;
    private const string TRANSPLANT_DATE_KEY = "transplant_date";
    
    public EventGeneratorService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
    }
    
    public async Task SetTransplantDateAsync(DateTime transplantDate)
    {
        await _localStorage.SetItemAsync(TRANSPLANT_DATE_KEY, new TransplantDateModel { Date = transplantDate });
    }
    
    public async Task<DateTime> GetTransplantDateAsync()
    {
        var model = await _localStorage.GetItemAsync<TransplantDateModel>(TRANSPLANT_DATE_KEY);
        return model?.Date ?? DateTime.Today;
    }
    
    public async Task<IEnumerable<HairTransplantEvent>> GenerateEventsForDateAsync(DateTime date)
    {
        var transplantDate = await GetTransplantDateAsync();
        var daysAfterTransplant = (date.Date - transplantDate.Date).Days;
        
        var events = new List<HairTransplantEvent>();
        
        if (daysAfterTransplant < 0)
            return events;
            
        if (daysAfterTransplant == 0)
            events.AddRange(await GenerateTransplantDayEventsAsync(date));
        else if (daysAfterTransplant <= 7)
            events.AddRange(await GenerateFirstWeekEventsAsync(date));
        else if (daysAfterTransplant <= 14)
            events.AddRange(await GenerateSecondWeekEventsAsync(date));
        else if (daysAfterTransplant <= 30)
            events.AddRange(await GenerateFirstMonthEventsAsync(date));
        else
            events.AddRange(await GenerateLaterEventsAsync(date));
            
        return events;
    }
    
    public async Task<IEnumerable<HairTransplantEvent>> GenerateEventsForRangeAsync(DateTime startDate, DateTime endDate)
    {
        var events = new List<HairTransplantEvent>();
        var currentDate = startDate.Date;
        
        while (currentDate <= endDate.Date)
        {
            events.AddRange(await GenerateEventsForDateAsync(currentDate));
            currentDate = currentDate.AddDays(1);
        }
        
        return events;
    }
    
    public async Task<IEnumerable<HairTransplantEvent>> GenerateInitialEventsAsync()
    {
        var transplantDate = await GetTransplantDateAsync();
        return await GenerateEventsForRangeAsync(transplantDate, transplantDate.AddMonths(1));
    }
    
    public async Task<int> GetCurrentPhaseAsync()
    {
        var transplantDate = await GetTransplantDateAsync();
        var daysAfterTransplant = (DateTime.Today - transplantDate.Date).Days;
        
        if (daysAfterTransplant <= 0) return 0;
        if (daysAfterTransplant <= 7) return 1;
        if (daysAfterTransplant <= 14) return 2;
        if (daysAfterTransplant <= 30) return 3;
        return 4;
    }
    
    private async Task<IEnumerable<HairTransplantEvent>> GenerateTransplantDayEventsAsync(DateTime date)
    {
        return new List<HairTransplantEvent>
        {
            new HairTransplantEvent
            {
                Id = Guid.NewGuid(),
                Title = "Принять антибиотик",
                Description = "Начать курс антибиотиков по схеме",
                StartDate = date.Add(new TimeSpan(9, 0, 0)),
                Type = EventType.Medication,
                Priority = EventPriority.High,
                CreatedAt = DateTime.UtcNow
            },
            new HairTransplantEvent
            {
                Id = Guid.NewGuid(),
                Title = "Сделать первое фото",
                Description = "Фото зоны пересадки и донорской зоны",
                StartDate = date.Add(new TimeSpan(12, 0, 0)),
                Type = EventType.Photo,
                Priority = EventPriority.High,
                CreatedAt = DateTime.UtcNow
            },
            new HairTransplantEvent
            {
                Id = Guid.NewGuid(),
                Title = "Не трогать зону пересадки",
                Description = "Не прикасаться, не расчесывать, не носить тесные головные уборы",
                StartDate = date,
                EndDate = date.AddDays(10),
                Type = EventType.Warning,
                Priority = EventPriority.Critical,
                CreatedAt = DateTime.UtcNow
            }
        };
    }
    
    private async Task<IEnumerable<HairTransplantEvent>> GenerateFirstWeekEventsAsync(DateTime date)
    {
        return new List<HairTransplantEvent>
        {
            new HairTransplantEvent
            {
                Id = Guid.NewGuid(),
                Title = "Ежедневный фотоотчет",
                Description = "Снимать общие планы и макросъемку зоны пересадки",
                StartDate = date.Add(new TimeSpan(15, 0, 0)),
                Type = EventType.Photo,
                Priority = EventPriority.High,
                CreatedAt = DateTime.UtcNow
            },
            new HairTransplantEvent
            {
                Id = Guid.NewGuid(),
                Title = "Принять антибиотик",
                Description = "Продолжение курса антибиотиков",
                StartDate = date.Add(new TimeSpan(9, 0, 0)),
                Type = EventType.Medication,
                Priority = EventPriority.High,
                CreatedAt = DateTime.UtcNow
            }
        };
    }
    
    private async Task<IEnumerable<HairTransplantEvent>> GenerateSecondWeekEventsAsync(DateTime date)
    {
        return new List<HairTransplantEvent>
        {
            new HairTransplantEvent
            {
                Id = Guid.NewGuid(),
                Title = "Фотоотчет",
                Description = "Еженедельная фотофиксация результатов",
                StartDate = date.Add(new TimeSpan(15, 0, 0)),
                Type = EventType.Photo,
                Priority = EventPriority.Normal,
                CreatedAt = DateTime.UtcNow
            }
        };
    }
    
    private async Task<IEnumerable<HairTransplantEvent>> GenerateFirstMonthEventsAsync(DateTime date)
    {
        return new List<HairTransplantEvent>
        {
            new HairTransplantEvent
            {
                Id = Guid.NewGuid(),
                Title = "Контрольный осмотр",
                Description = "Посещение клиники для оценки результатов",
                StartDate = date.Add(new TimeSpan(10, 0, 0)),
                Type = EventType.MedicalVisit,
                Priority = EventPriority.High,
                CreatedAt = DateTime.UtcNow
            }
        };
    }
    
    private async Task<IEnumerable<HairTransplantEvent>> GenerateLaterEventsAsync(DateTime date)
    {
        return new List<HairTransplantEvent>
        {
            new HairTransplantEvent
            {
                Id = Guid.NewGuid(),
                Title = "Ежемесячный фотоотчет",
                Description = "Фотофиксация роста волос",
                StartDate = date.Add(new TimeSpan(15, 0, 0)),
                Type = EventType.Photo,
                Priority = EventPriority.Normal,
                CreatedAt = DateTime.UtcNow
            }
        };
    }
} 