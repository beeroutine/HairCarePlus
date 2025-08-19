using System;
using System.Threading.Tasks;
using HairCarePlus.Shared.Communication;
using HairCarePlus.Shared.Common;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Client.Clinic.Infrastructure.Network.Events;

public sealed class SignalREventsSubscription : IEventsSubscription
{
    private readonly ILogger<SignalREventsSubscription> _logger;
    private HubConnection? _connection;

    public SignalREventsSubscription(ILogger<SignalREventsSubscription> logger)
    {
        _logger = logger;
    }

    public event EventHandler<PhotoReportDto>? PhotoReportAdded;
    public event EventHandler<PhotoCommentDto>? PhotoCommentAdded;
    public event EventHandler<PhotoReportSetDto>? PhotoReportSetAdded;
    public event EventHandler<RestrictionDto>? RestrictionChangedEvent;
    public event EventHandler<CalendarTaskDto>? CalendarTaskChangedEvent;

    public async Task ConnectAsync(string patientId)
    {
        if (_connection != null && _connection.State != HubConnectionState.Disconnected)
            return;

        var baseUrl = HairCarePlus.Shared.Common.EnvironmentHelper.GetBaseApiUrl();
        _connection = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}/events?patientId={patientId}")
            .WithAutomaticReconnect()
            .ConfigureLogging(l => l.SetMinimumLevel(LogLevel.Information))
            .Build();

        _connection.On<string, PhotoReportDto>("PhotoReportAdded", (pid, dto) =>
        {
            if (pid == patientId)
                PhotoReportAdded?.Invoke(this, dto);
        });

        _connection.On<string, string, PhotoCommentDto>("PhotoCommentAdded", (pid, reportId, dto) =>
        {
            if (pid == patientId)
                PhotoCommentAdded?.Invoke(this, dto);
        });

        _connection.On<string, PhotoReportSetDto>("PhotoReportSetAdded", (pid, set) =>
        {
            if (pid == patientId)
            {
                _logger.LogInformation("SignalR: PhotoReportSetAdded received for patient {PatientId}. SetId={SetId} ItemCount={Count}", pid, set?.Id, set?.Items?.Count ?? 0);
                PhotoReportSetAdded?.Invoke(this, set);
            }
        });

        _connection.On<RestrictionDto>("RestrictionChanged", dto =>
        {
            RestrictionChangedEvent?.Invoke(this, dto);
        });

        _connection.On<CalendarTaskDto>("CalendarTaskChanged", dto =>
        {
            CalendarTaskChangedEvent?.Invoke(this, dto);
        });

        try
        {
            await _connection.StartAsync();
            _logger.LogInformation("Connected to Events hub for patient {PatientId}", patientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to events hub");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
            await _connection.DisposeAsync();
    }
} 