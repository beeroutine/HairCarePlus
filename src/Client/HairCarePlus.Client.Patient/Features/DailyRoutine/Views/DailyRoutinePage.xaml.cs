using HairCarePlus.Client.Patient.Features.DailyRoutine.ViewModels;

namespace HairCarePlus.Client.Patient.Features.DailyRoutine.Views;

public partial class DailyRoutinePage : ContentPage
{
    public DailyRoutinePage()
    {
        InitializeComponent();
    }

    public DailyRoutinePage(DailyRoutineViewModel viewModel) : this()
    {
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is DailyRoutineViewModel viewModel)
        {
            await viewModel.LoadDataAsync();
        }
    }
} 