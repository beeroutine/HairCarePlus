namespace HairCarePlus.Client.Clinic.Features.Patient.Views;

public partial class PatientPage : ContentPage
{
    public PatientPage()
    {
        InitializeComponent();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
} 