using System;
using System.Threading.Tasks;
using HairCarePlus.Shared.Communication;

namespace HairCarePlus.Client.Clinic.Infrastructure.Network.Events;

public interface IEventsSubscription : IAsyncDisposable
{
    Task ConnectAsync(string patientId);

    event EventHandler<PhotoReportDto>? PhotoReportAdded;
    event EventHandler<PhotoCommentDto>? PhotoCommentAdded;
} 