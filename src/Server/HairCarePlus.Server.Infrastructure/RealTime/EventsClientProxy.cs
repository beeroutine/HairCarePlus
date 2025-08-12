using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using HairCarePlus.Shared.Communication.Events;
using HairCarePlus.Shared.Communication;

namespace HairCarePlus.Server.Infrastructure.RealTime;

public sealed class EventsClientProxy : IEventsClient
{
    private readonly IHubContext<EventsHub, IEventsClient> _hubContext;

    public EventsClientProxy(IHubContext<EventsHub, IEventsClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PhotoReportAdded(string patientId, PhotoReportDto report)
    {
        return _hubContext.Clients.Group(PatientEventNames.GroupName(patientId)).PhotoReportAdded(patientId, report);
    }

    public Task PhotoCommentAdded(string patientId, string photoReportId, PhotoCommentDto comment)
    {
        return _hubContext.Clients.Group(PatientEventNames.GroupName(patientId)).PhotoCommentAdded(patientId, photoReportId, comment);
    }

    public Task PhotoReportSetAdded(string patientId, PhotoReportSetDto set)
    {
        return _hubContext.Clients.Group(PatientEventNames.GroupName(patientId)).PhotoReportSetAdded(patientId, set);
    }
} 