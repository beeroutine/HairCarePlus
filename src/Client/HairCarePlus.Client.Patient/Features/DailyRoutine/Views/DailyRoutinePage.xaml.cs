using HairCarePlus.Client.Patient.Features.DailyRoutine.ViewModels;

namespace HairCarePlus.Client.Patient.Features.DailyRoutine.Views;

public partial class DailyRoutinePage : ContentPage
{
    private readonly DailyRoutineViewModel _viewModel;

    public DailyRoutinePage(DailyRoutineViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataCommand.ExecuteAsync(null);
    }
} 