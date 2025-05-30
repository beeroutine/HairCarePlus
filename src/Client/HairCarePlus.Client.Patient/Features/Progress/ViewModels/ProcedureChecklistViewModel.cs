using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Client.Patient.Features.Progress.ViewModels;

public partial class ProcedureChecklistViewModel : ObservableObject
{
    private readonly ILogger<ProcedureChecklistViewModel> _logger;

    public ProcedureChecklistViewModel(ILogger<ProcedureChecklistViewModel> logger)
    {
        _logger = logger;
        Procedures = new ObservableCollection<ProcedureCheck>
        {
            new() { Title = "Wash", Date = DateOnly.FromDateTime(DateTime.Now), IsDone = false },
            new() { Title = "Spray", Date = DateOnly.FromDateTime(DateTime.Now), IsDone = false },
            new() { Title = "Massage", Date = DateOnly.FromDateTime(DateTime.Now), IsDone = false }
        };
    }

    public ObservableCollection<ProcedureCheck> Procedures { get; }

    [RelayCommand]
    private void Close(object? popup)
    {
        if (popup is CommunityToolkit.Maui.Views.Popup p)
            p.Close();
    }
} 