using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Domain.Entities;
using HairCarePlus.Client.Patient.Infrastructure.Storage;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services;

public class RestrictionService : IRestrictionService
{
    private readonly ILocalStorageService _storageService;
    private const string RESTRICTIONS_KEY = "hair_transplant_restrictions";

    public RestrictionService(ILocalStorageService storageService)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
    }

    public async Task<IEnumerable<HairTransplantEvent>> GetActiveRestrictionsAsync()
    {
        var restrictions = await GetAllRestrictionsAsync();
        return restrictions.Where(r => r.EndDate >= DateTime.Now);
    }

    public async Task<bool> HasActiveRestrictionsForDateAsync(DateTime date)
    {
        var restrictions = await GetActiveRestrictionsAsync();
        return restrictions.Any(r => r.StartDate <= date && r.EndDate >= date);
    }

    public async Task<IEnumerable<HairTransplantEvent>> GetRestrictionsForActivityTypeAsync(EventType activityType)
    {
        var restrictions = await GetActiveRestrictionsAsync();
        return restrictions.Where(r => r.Type == activityType);
    }

    public async Task<HairTransplantEvent> AddRestrictionAsync(HairTransplantEvent restriction)
    {
        var restrictions = (await GetAllRestrictionsAsync()).ToList();
        restrictions.Add(restriction);
        await SaveRestrictionsAsync(restrictions);
        return restriction;
    }

    public async Task<HairTransplantEvent> UpdateRestrictionAsync(HairTransplantEvent restriction)
    {
        var restrictions = (await GetAllRestrictionsAsync()).ToList();
        var index = restrictions.FindIndex(r => r.Id == restriction.Id);
        if (index != -1)
        {
            restrictions[index] = restriction;
            await SaveRestrictionsAsync(restrictions);
            return restriction;
        }
        throw new KeyNotFoundException($"Restriction with ID {restriction.Id} not found");
    }

    public async Task<bool> RemoveRestrictionAsync(Guid id)
    {
        var restrictions = (await GetAllRestrictionsAsync()).ToList();
        var removed = restrictions.RemoveAll(r => r.Id == id);
        if (removed > 0)
        {
            await SaveRestrictionsAsync(restrictions);
            return true;
        }
        return false;
    }

    private async Task<IEnumerable<HairTransplantEvent>> GetAllRestrictionsAsync()
    {
        try
        {
            var restrictions = await _storageService.GetItemAsync<List<HairTransplantEvent>>(RESTRICTIONS_KEY);
            return restrictions ?? new List<HairTransplantEvent>();
        }
        catch
        {
            return new List<HairTransplantEvent>();
        }
    }

    private Task SaveRestrictionsAsync(IEnumerable<HairTransplantEvent> restrictions)
    {
        return _storageService.SetItemAsync(RESTRICTIONS_KEY, restrictions.ToList());
    }
} 