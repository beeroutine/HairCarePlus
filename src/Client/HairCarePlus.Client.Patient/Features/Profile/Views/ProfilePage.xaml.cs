using HairCarePlus.Client.Patient.Common;
using HairCarePlus.Client.Patient.Features.Profile.ViewModels;
using HairCarePlus.Client.Patient.Infrastructure.Services;

namespace HairCarePlus.Client.Patient.Features.Profile.Views;

public partial class ProfilePage : BaseContentPage
{
    public ProfilePage(ProfileViewModel viewModel, INavigationService navigationService)
        : base(navigationService)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ViewModelBase viewModel)
        {
            await viewModel.LoadDataAsync();
        }
    }
} 