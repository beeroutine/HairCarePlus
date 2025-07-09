using Microsoft.Maui.Controls;
using System.Collections.Generic;

namespace HairCarePlus.Client.Clinic.Features.Patient.Views;

public partial class PatientProgressPage : ContentPage, IQueryAttributable
{
    private readonly ViewModels.PatientPageViewModel _vm;

    public PatientProgressPage(ViewModels.PatientPageViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("patientId", out var idObj) && idObj is string id)
        {
            _vm.PatientId = id;
        }
        // стартуем загрузку, когда параметр установлен
        _ = _vm.LoadCommand.ExecuteAsync(null);
    }
} 