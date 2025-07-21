namespace HairCarePlus.Server.Infrastructure;

public class DeliveryOptions
{
    /// <summary>
    /// Time-to-live for PhotoReport entities on the server (in days).
    /// </summary>
    public int PhotoReportTtlDays { get; set; } = 14;
} 