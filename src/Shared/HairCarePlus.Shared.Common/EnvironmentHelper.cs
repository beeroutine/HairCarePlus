namespace HairCarePlus.Shared.Common;

public static class EnvironmentHelper
{
    private const string DefaultBaseUrl = "http://192.168.1.6:5281";

    /// <summary>
    /// Returns base URL for HairCare+ API. Tries the CHAT_BASE_URL environment variable first, otherwise falls back to LAN IP.
    /// </summary>
    public static string GetBaseApiUrl()
    {
        return Environment.GetEnvironmentVariable("CHAT_BASE_URL") ?? DefaultBaseUrl;
    }
} 