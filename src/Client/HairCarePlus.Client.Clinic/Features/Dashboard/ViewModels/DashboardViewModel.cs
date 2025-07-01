using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HairCarePlus.Client.Clinic.Features.Dashboard.Models;
using HairCarePlus.Client.Clinic.Features.Chat.Views;
using Microsoft.Maui.Controls;
using System.Linq;
using HairCarePlus.Client.Clinic.Infrastructure.Features.Patient;

namespace HairCarePlus.Client.Clinic.Features.Dashboard.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IPatientService _patientService;

    public ObservableCollection<PatientSummary> Patients { get; } = new();

    [ObservableProperty]
    private double _globalProgress;

    public IAsyncRelayCommand LoadCommand { get; }
    public IRelayCommand<PatientSummary> OpenChatCommand { get; }
    public IRelayCommand<PatientSummary> CallCommand { get; }

    public DashboardViewModel(IPatientService patientService)
    {
        _patientService = patientService;
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
        var dtoList = await _patientService.GetPatientsAsync();
        Patients.Clear();
        foreach (var dto in dtoList)
        {
            Patients.Add(new PatientSummary
            {
                Id = dto.Id,
                Name = dto.Name,
                DayProgress = dto.DayProgress,
                PhotoMissing = dto.PhotoMissing,
                UnreadCount = dto.UnreadCount,
                AvatarUrl = dto.AvatarUrl
            });
        }
        GlobalProgress = Patients.Count == 0 ? 0 : Patients.Average(p => p.DayProgress);
    }
} 