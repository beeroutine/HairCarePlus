using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HairCarePlus.Shared.Common.CQRS;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using HairCarePlus.Client.Patient.Features.PhotoCapture.Domain.Entities;
using HairCarePlus.Client.Patient.Features.PhotoCapture.Application.Queries;
using System.Linq;

namespace HairCarePlus.Client.Patient.Features.PhotoCapture.ViewModels;

public partial class PhotoCaptureViewModel : ObservableObject
{
    private readonly ICommandBus _commandBus;
    private readonly IQueryBus _queryBus;
    private readonly ILogger<PhotoCaptureViewModel> _logger;

    public PhotoCaptureViewModel(ICommandBus commandBus,
                                 IQueryBus queryBus,
                                 ILogger<PhotoCaptureViewModel> logger)
    {
        _commandBus = commandBus;
        _queryBus = queryBus;
        _logger = logger;

        _ = LoadTemplatesAsync();
    }

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private IList<CaptureTemplate> _templates = new List<CaptureTemplate>();

    [ObservableProperty]
    private CaptureTemplate? _selectedTemplate;

    [ObservableProperty]
    private int _lux = 320;

    [RelayCommand]
    private async Task Capture()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            await _commandBus.SendAsync(new HairCarePlus.Client.Patient.Features.PhotoCapture.Application.Commands.CapturePhotoCommand());
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Capture failed");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SelectTemplate(string id)
    {
        if (Templates == null) return;
        SelectedTemplate = Templates.FirstOrDefault(t => t.Id == id);
    }

    [RelayCommand]
    private async Task SwitchCamera()
    {
        await _commandBus.SendAsync(new HairCarePlus.Client.Patient.Features.PhotoCapture.Application.Commands.CapturePhotoCommand());
    }

    partial void OnSelectedTemplateChanged(CaptureTemplate? oldValue, CaptureTemplate? newValue)
    {
        // future logic maybe update overlay etc.
    }

    public void MarkCurrentAsCaptured()
    {
        if (SelectedTemplate != null) SelectedTemplate.IsCaptured = true;
    }

    private async Task LoadTemplatesAsync()
    {
        try
        {
            var list = await _queryBus.SendAsync<IReadOnlyList<CaptureTemplate>>(new GetCaptureTemplatesQuery());
            Templates = new List<CaptureTemplate>(list);
            SelectedTemplate = Templates.FirstOrDefault();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error loading templates");
        }
    }
} 