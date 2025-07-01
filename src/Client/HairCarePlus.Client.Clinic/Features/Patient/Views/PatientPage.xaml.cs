namespace HairCarePlus.Client.Clinic.Features.Patient.Views;

public partial class PatientPage : ContentPage
{
    private readonly ViewModels.PatientPageViewModel _vm;

    public PatientPage(ViewModels.PatientPageViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
        Loaded += async (s,e) => await _vm.LoadCommand.ExecuteAsync(null);
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
} 