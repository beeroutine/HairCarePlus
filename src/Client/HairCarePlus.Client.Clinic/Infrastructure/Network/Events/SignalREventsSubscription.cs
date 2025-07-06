using System;
using System.Threading.Tasks;
using HairCarePlus.Shared.Communication;
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

    public async Task ConnectAsync(string patientId)
    {
        if (_connection != null && _connection.State != HubConnectionState.Disconnected)
            return;

        var baseUrl = Environment.GetEnvironmentVariable("CHAT_BASE_URL") ?? "http://10.153.34.67:5281";
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