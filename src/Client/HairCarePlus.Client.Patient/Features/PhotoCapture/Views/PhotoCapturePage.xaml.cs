using Microsoft.Maui.Controls;
using HairCarePlus.Client.Patient.Features.PhotoCapture.ViewModels;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Features.PhotoCapture.Views;

public partial class PhotoCapturePage : ContentPage
{
    private readonly PhotoCaptureViewModel _viewModel;
    private readonly ILogger<PhotoCapturePage> _logger;

    public PhotoCapturePage(PhotoCaptureViewModel viewModel, ILogger<PhotoCapturePage> logger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _logger = logger;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Если понадобится предварительный просмотр камеры, можно запустить здесь через Messaging или DI
        await Task.CompletedTask;
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await Task.CompletedTask;
    }

    private void OnZoneChecked(object? sender, CheckedChangedEventArgs e)
    {
        if (e.Value != true) return;
        if (sender is RadioButton rb && rb.Content is string label)
        {
            var id = label switch
            {
                "Фронт" => "front",
                "Темя" => "top",
                "Затылок" => "back",
                _ => null
            };
            if (id != null)
            {
                _viewModel.SelectTemplateCommand.Execute(id);
            }
        }
    }
} 