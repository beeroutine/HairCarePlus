using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HairCarePlus.Client.Clinic.Features.Dashboard.Models;
using HairCarePlus.Client.Clinic.Features.Chat.Views;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Clinic.Features.Dashboard.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    public ObservableCollection<PatientSummary> Patients { get; } = new();

    [ObservableProperty]
    private double _globalProgress;

    public IAsyncRelayCommand LoadCommand { get; }
    public IRelayCommand<PatientSummary> OpenChatCommand { get; }
    public IRelayCommand<PatientSummary> CallCommand { get; }

    public DashboardViewModel()
    {
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        OpenChatCommand = new RelayCommand<PatientSummary>(async p =>
        {
            if (p is null) return;
            await Shell.Current.GoToAsync(nameof(ChatPage), true, new Dictionary<string, object>
            {
                { "patientId", p.Id }
            });
        });

        CallCommand = new RelayCommand<PatientSummary>(p =>
        {
            // TODO: integrate platform dialer / tel: protocol
            // For now just log or ignore
        });
    }

    private async Task LoadAsync()
    {
        // TODO: fetch from API, now mock
        await Task.Delay(300);
        Patients.Clear();
        Patients.Add(new PatientSummary
        {
            Id = "p1",
            Name = "Анна Петрова",
            DayProgress = 0.8,
            PhotoMissing = false,
            UnreadCount = 2,
            AvatarUrl = null
        });
        Patients.Add(new PatientSummary
        {
            Id = "p2",
            Name = "Иван Иванов",
            DayProgress = 0.3,
            PhotoMissing = true,
            UnreadCount = 0,
            AvatarUrl = null
        });
        GlobalProgress = Patients.Count == 0 ? 0 : Patients.Average(p => p.DayProgress);
    }
} 