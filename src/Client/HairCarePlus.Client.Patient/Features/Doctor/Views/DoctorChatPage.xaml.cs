using HairCarePlus.Client.Patient.Features.Doctor.ViewModels;

namespace HairCarePlus.Client.Patient.Features.Doctor.Views;

public partial class DoctorChatPage : ContentPage
{
    public DoctorChatPage()
    {
        InitializeComponent();
    }

    public DoctorChatPage(DoctorChatViewModel viewModel) : this()
    {
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is DoctorChatViewModel viewModel)
        {
            await viewModel.LoadDataAsync();
        }
    }
} 