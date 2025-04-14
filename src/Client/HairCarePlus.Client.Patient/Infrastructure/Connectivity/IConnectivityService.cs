namespace HairCarePlus.Client.Patient.Infrastructure.Connectivity;

public interface IConnectivityService
{
    bool IsConnected { get; }
    event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged;
}

public class ConnectivityChangedEventArgs : EventArgs
{
    public bool IsConnected { get; }

    public ConnectivityChangedEventArgs(bool isConnected)
    {
        IsConnected = isConnected;
    }
} 