using HairCarePlus.Client.Clinic.Features.Dashboard.ViewModels;
using HairCarePlus.Client.Clinic.Features.Patient.Views;

namespace HairCarePlus.Client.Clinic.Features.Dashboard.Views;

public partial class DashboardPage : ContentPage
{
    public DashboardPage(DashboardViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        Loaded += async (s,e) => await vm.LoadCommand.ExecuteAsync(null);
        PatientsCollection.SelectionChanged += async (s,e) =>
        {
            if(e.CurrentSelection.FirstOrDefault() is Features.Dashboard.Models.PatientSummary selected)
            {
                await Shell.Current.GoToAsync(nameof(PatientPage), true, new Dictionary<string,object>{{"patientId", selected.Id}});
                PatientsCollection.SelectedItem = null;
            }
        };
    }
} 