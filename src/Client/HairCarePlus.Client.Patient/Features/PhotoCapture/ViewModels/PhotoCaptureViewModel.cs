using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HairCarePlus.Shared.Common.CQRS;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using HairCarePlus.Client.Patient.Features.PhotoCapture.Domain.Entities;
using HairCarePlus.Client.Patient.Features.PhotoCapture.Application.Queries;
using System.Linq;
using Microsoft.Maui.Controls;
using HairCarePlus.Client.Patient.Features.Sync.Infrastructure;
using System.Text.Json;

namespace HairCarePlus.Client.Patient.Features.PhotoCapture.ViewModels;

public partial class PhotoCaptureViewModel : ObservableObject
{
    private readonly ICommandBus _commandBus;
    private readonly IQueryBus _queryBus;
    private readonly ILogger<PhotoCaptureViewModel> _logger;
    private readonly IOutboxRepository _outbox;

    public PhotoCaptureViewModel(ICommandBus commandBus,
                                 IQueryBus queryBus,
                                 ILogger<PhotoCaptureViewModel> logger,
                                 IOutboxRepository outbox)
    {
        _commandBus = commandBus;
        _queryBus = queryBus;
        _logger = logger;
        _outbox = outbox;

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

    [ObservableProperty]
    private string? _lastPhotoPath;

    [ObservableProperty]
    private string? _instructionText;

    [ObservableProperty]
    private bool _showInstruction;

    public enum CameraFacing
    {
        Front,
        Back
    }

    [ObservableProperty]
    private CameraFacing _facing = CameraFacing.Front;

    [RelayCommand]
    private void ToggleFacing()
    {
        _logger.LogInformation($"ToggleFacingCommand called. Current facing: {_facing}");
        Facing = Facing == CameraFacing.Front ? CameraFacing.Back : CameraFacing.Front;
        _logger.LogInformation($"New facing: {_facing}");
        // TODO: publish message or invoke service to switch actual camera in view.
    }

    [RelayCommand]
    private async Task Capture()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            await _commandBus.SendAsync(new HairCarePlus.Client.Patient.Features.PhotoCapture.Application.Commands.CapturePhotoCommand());

            // enqueue to outbox
            var dto = new HairCarePlus.Shared.Communication.PhotoReportCreateDto
            {
                PatientId = "8f8c7e0b-1234-4e78-a8cc-ff0011223344",
                LocalFilePath = _lastPhotoPath,
                CaptureDate = DateTime.UtcNow
            };

            var item = new Features.Sync.Domain.Entities.OutboxItem
            {
                EntityType = "PhotoReport",
                PayloadJson = JsonSerializer.Serialize(dto),
                LocalEntityId = Guid.NewGuid().ToString(),
                ModifiedAtUtc = DateTime.UtcNow
            };
            await _outbox.AddAsync(item);
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

    partial void OnSelectedTemplateChanged(CaptureTemplate? oldValue, CaptureTemplate? newValue)
    {
        if (newValue != null)
        {
            InstructionText = $"Сфотографируйте {newValue.Name}";
            ShowInstruction = true;
        }
    }

    public void MarkCurrentAsCaptured()
    {
        if (SelectedTemplate != null)
        {
            SelectedTemplate.IsCaptured = true;
            ShowInstruction = false;
            // Wait briefly then move to next template and show again
            global::Microsoft.Maui.Controls.Application.Current?.Dispatcher.Dispatch(async () =>
            {
                await Task.Delay(600); // match fade time
                var currentIndex = Templates.IndexOf(SelectedTemplate);
                if (currentIndex + 1 < Templates.Count)
                {
                    SelectedTemplate = Templates[currentIndex + 1];
                }
            });
        }
    }

    [RelayCommand]
    private async Task Back()
    {
        await Shell.Current.GoToAsync("//today");
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