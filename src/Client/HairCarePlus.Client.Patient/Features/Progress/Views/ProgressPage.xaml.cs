using HairCarePlus.Client.Patient.Features.Progress.ViewModels;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Client.Patient.Features.Progress.Views;

public partial class ProgressPage : ContentPage
{
    public ProgressPage(ProgressViewModel vm, ILogger<ProgressPage> logger)
    {
        InitializeComponent();
        BindingContext = vm;
        logger.LogInformation("ProgressPage created");
    }
} 