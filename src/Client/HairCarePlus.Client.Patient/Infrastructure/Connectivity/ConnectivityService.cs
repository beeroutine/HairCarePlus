using Microsoft.Maui.Networking;

namespace HairCarePlus.Client.Patient.Infrastructure.Connectivity;

public class ConnectivityService : IConnectivityService
{
    private readonly IConnectivity _connectivity;

    public bool IsConnected => _connectivity.NetworkAccess == NetworkAccess.Internet;

    public event EventHandler<ConnectivityChangedEventArgs>? ConnectivityChanged;

    public ConnectivityService(IConnectivity connectivity)
    {
        _connectivity = connectivity;
        _connectivity.ConnectivityChanged += OnMauiConnectivityChanged;
    }

    private void OnMauiConnectivityChanged(object? sender, EventArgs e)
    {
        ConnectivityChanged?.Invoke(this, new ConnectivityChangedEventArgs(_connectivity.NetworkAccess == NetworkAccess.Internet));
    }

    ~ConnectivityService()
    {
        _connectivity.ConnectivityChanged -= OnMauiConnectivityChanged;
    }
} 