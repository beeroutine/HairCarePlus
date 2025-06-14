using HairCarePlus.Client.Patient.Features.Progress.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Progress.Views;

public partial class ProgressPage : ContentPage
{
    public ProgressPage(ProgressViewModel vm, ILogger<ProgressPage> logger)
    {
        InitializeComponent();
        BindingContext = vm;
        logger.LogInformation("ProgressPage created");

        Microsoft.Maui.Controls.Application.Current.RequestedThemeChanged += OnRequestedThemeChanged;
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        // Force rebind to recreate DataTemplates with correct tint
        var src = RestrictionCollectionView.ItemsSource;
        RestrictionCollectionView.ItemsSource = null;
        RestrictionCollectionView.ItemsSource = src;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Microsoft.Maui.Controls.Application.Current.RequestedThemeChanged -= OnRequestedThemeChanged;
    }
} 