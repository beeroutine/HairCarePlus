using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using HairCarePlus.Shared.Communication.Events;
using HairCarePlus.Shared.Communication;

namespace HairCarePlus.Server.Infrastructure.RealTime;

/// <summary>
/// SignalR hub for push-events. Clients join group "patient-{id}" to receive updates.
/// </summary>
[AllowAnonymous] // TODO: Add auth later
public sealed class EventsHub : Hub<IEventsClient>
{
    public override async Task OnConnectedAsync()
    {
        var pid = Context.GetHttpContext()?.Request.Query["patientId"].ToString();
        if (!string.IsNullOrWhiteSpace(pid))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"patient-{pid}");
        }
        await base.OnConnectedAsync();
    }
} 