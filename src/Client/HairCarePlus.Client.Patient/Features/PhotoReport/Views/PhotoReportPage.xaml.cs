using HairCarePlus.Client.Patient.Features.PhotoReport.ViewModels;

namespace HairCarePlus.Client.Patient.Features.PhotoReport.Views;

public partial class PhotoReportPage : ContentPage
{
    public PhotoReportPage()
    {
        InitializeComponent();
    }

    public PhotoReportPage(PhotoReportViewModel viewModel) : this()
    {
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is PhotoReportViewModel viewModel)
        {
            await viewModel.LoadDataAsync();
        }
    }
} 