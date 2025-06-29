using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace HairCarePlus.Client.Clinic.Features.Patient.ViewModels;

public partial class PatientPageViewModel : ObservableObject, IQueryAttributable
{
    // Simple DTOs
    public record RestrictionTimer(string Icon, int DaysRemaining, double Progress);
    public record PhotoEntry(string ImageUrl, string Comment);

    [ObservableProperty] private string _patientId = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _avatarUrl;
    [ObservableProperty] private double _dayProgress;

    public ObservableCollection<RestrictionTimer> Restrictions { get; } = new();
    public ObservableCollection<PhotoEntry> Feed { get; } = new();

    public AsyncRelayCommand LoadCommand { get; }

    public PatientPageViewModel()
    {
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("patientId", out var idObj) && idObj is string id)
        {
            PatientId = id;
            _ = LoadCommand.ExecuteAsync(null);
        }
    }

    private async Task LoadAsync()
    {
        // TODO: replace with real API call
        await Task.Delay(300);
        Name = "Анна Петрова";
        DayProgress = 0.8;
        AvatarUrl = null;
        Restrictions.Clear();
        Restrictions.Add(new RestrictionTimer("🚫", 5, 0.6));
        Restrictions.Add(new RestrictionTimer("💊", 2, 0.9));
        Feed.Clear();
        Feed.Add(new PhotoEntry("https://placehold.co/300", "Отличный прогресс!"));
    }
} 