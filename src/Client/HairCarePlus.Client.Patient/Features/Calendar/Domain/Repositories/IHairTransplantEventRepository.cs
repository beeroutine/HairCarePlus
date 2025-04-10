using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Domain.Entities;
using HairCarePlus.Client.Patient.Infrastructure.Storage.Repositories;

namespace HairCarePlus.Client.Patient.Features.Calendar.Domain.Repositories;

public interface IHairTransplantEventRepository : IBaseRepository<HairTransplantEvent>
{
    Task<IEnumerable<HairTransplantEvent>> GetEventsForDateAsync(DateTime date);
    Task<IEnumerable<HairTransplantEvent>> GetEventsForRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<HairTransplantEvent>> GetPendingEventsAsync();
    Task MarkEventAsCompletedAsync(Guid eventId);
} 