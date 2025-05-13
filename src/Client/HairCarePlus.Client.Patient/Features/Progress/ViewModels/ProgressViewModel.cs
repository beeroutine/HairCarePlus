using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;
using HairCarePlus.Shared.Common.CQRS;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Input;
using ICommand = System.Windows.Input.ICommand;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using HairCarePlus.Client.Patient.Features.Progress.Views;
using CommunityToolkit.Maui.Views;

namespace HairCarePlus.Client.Patient.Features.Progress.ViewModels;

public partial class ProgressViewModel : ObservableObject
{
    private readonly IQueryBus _queryBus;
    private readonly ILogger<ProgressViewModel> _logger;
    private readonly IServiceProvider _sp;

    private const int DefaultDaysRange = 7;

    public ProgressViewModel(IQueryBus queryBus, ILogger<ProgressViewModel> logger, IServiceProvider sp)
    {
        _queryBus = queryBus;
        _logger = logger;
        _sp = sp;

        RestrictionTimers = new ObservableCollection<RestrictionTimer>();
        Feed = new ObservableCollection<ProgressFeedItem>();

        _ = LoadAsync();
    }

    public ObservableCollection<RestrictionTimer> RestrictionTimers { get; }
    public ObservableCollection<ProgressFeedItem> Feed { get; }

    // Selected feed item (for future insights)
    [ObservableProperty]
    private ProgressFeedItem? _selectedFeedItem;

    public ICommand AddPhotoCommand => new RelayCommand(async () =>
    {
        try
        {
            await Shell.Current.GoToAsync("//camera");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Navigation to camera failed");
        }
    });

    public ICommand CompleteProcedureCommand => new RelayCommand(() =>
    {
        try
        {
            var vm = _sp.GetRequiredService<ProcedureChecklistViewModel>();
            var popup = new ProcedureChecklistPopup(vm);
            Shell.Current.CurrentPage?.ShowPopup(popup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open procedure checklist");
        }
    });

    public ICommand OpenInsightsCommand => new RelayCommand<AIReport>(report =>
    {
        if (report is null) return;
        try
        {
            var sheet = new InsightsSheet(report);
            Shell.Current.CurrentPage?.ShowPopup(sheet);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open insights sheet");
        }
    });

    private async Task LoadAsync()
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var from = today.AddDays(-(DefaultDaysRange - 1)); // inclusive
            var feedItems = await _queryBus.SendAsync(new Application.Queries.GetProgressFeedQuery(from, today));

            Feed.Clear();
            foreach (var item in feedItems)
                Feed.Add(item);

            // Use latest day restrictions for header until per-day selection implemented
            var todayItem = feedItems.FirstOrDefault();
            RestrictionTimers.Clear();
            foreach (var t in todayItem?.ActiveRestrictions ?? Enumerable.Empty<RestrictionTimer>())
                RestrictionTimers.Add(t);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading progress feed");
        }
    }
} 