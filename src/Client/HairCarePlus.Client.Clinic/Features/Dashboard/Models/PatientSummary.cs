namespace HairCarePlus.Client.Clinic.Features.Dashboard.Models;

public class PatientSummary
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? AvatarUrl { get; init; }
    public double DayProgress { get; set; } // 0..1
    public bool PhotoMissing { get; set; }
    public int UnreadCount { get; set; }

    // Commands delegated from DashboardViewModel for swipe actions
    public System.Windows.Input.ICommand? OpenChatCommand { get; init; }
    public System.Windows.Input.ICommand? CallCommand { get; init; }
} 