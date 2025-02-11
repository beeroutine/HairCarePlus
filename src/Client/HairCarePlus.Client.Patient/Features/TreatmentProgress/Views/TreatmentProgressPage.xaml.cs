using HairCarePlus.Client.Patient.Features.TreatmentProgress.ViewModels;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.TreatmentProgress.Views;

public partial class TreatmentProgressPage : ContentPage
{
    public TreatmentProgressPage()
    {
        InitializeComponent();
    }

    public TreatmentProgressPage(TreatmentProgressViewModel viewModel) : this()
    {
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is TreatmentProgressViewModel viewModel)
        {
            await viewModel.LoadDataAsync();
        }
    }
} 