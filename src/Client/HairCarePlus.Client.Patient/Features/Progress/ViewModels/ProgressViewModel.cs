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
using HairCarePlus.Client.Patient.Features.Progress.Selectors;
using CommunityToolkit.Mvvm.Messaging;
using HairCarePlus.Client.Patient.Features.PhotoCapture.Application.Messages;
using HairCarePlus.Client.Patient.Features.PhotoCapture.Views;
using HairCarePlus.Client.Patient.Features.Progress.Application.Messages;
using HairCarePlus.Client.Patient.Infrastructure.Services.Interfaces;
using HairCarePlus.Client.Patient.Features.Progress.Services.Interfaces;

namespace HairCarePlus.Client.Patient.Features.Progress.ViewModels;

public partial class ProgressViewModel : ObservableObject, IRecipient<PhotoCapturedMessage>, IRecipient<RestrictionsChangedMessage>
{
    private readonly IQueryBus _queryBus;
    private readonly ILogger<ProgressViewModel> _logger;
    private readonly IServiceProvider _sp;
    private readonly IProfileService _profileService;
    private readonly IProgressNavigationService _nav;

    private const int DefaultDaysRange = 7;
    private const int MaxVisibleRestrictionItems = 4;

    public ProgressViewModel(IQueryBus queryBus, ILogger<ProgressViewModel> logger, IServiceProvider sp, IProfileService profileService, IProgressNavigationService nav)
    {
        _queryBus = queryBus;
        _logger = logger;
        _sp = sp;
        _profileService = profileService;
        _nav = nav;

        RestrictionTimers = new ObservableCollection<RestrictionTimer>();
        Feed = new ObservableCollection<ProgressFeedItem>();
        VisibleRestrictionItems = new ObservableCollection<object>();

        _ = LoadAsync();

        // подписка на сообщение о новом фото
        WeakReferenceMessenger.Default.Register<PhotoCapturedMessage>(this);
        // подписка на изменение ограничений
        WeakReferenceMessenger.Default.Register<RestrictionsChangedMessage>(this);
    }

    public ObservableCollection<RestrictionTimer> RestrictionTimers { get; }
    public ObservableCollection<ProgressFeedItem> Feed { get; }
    public ObservableCollection<object> VisibleRestrictionItems { get; }

    /// <summary>
    /// Дата операции пациента (для годового таймлайна).
    /// TODO: загрузить из профиля.
    /// </summary>
    public DateTime SurgeryDate => _profileService.SurgeryDate;

    // Selected feed item (for future insights)
    [ObservableProperty]
    private ProgressFeedItem? _selectedFeedItem;

    public ICommand AddPhotoCommand => new RelayCommand(async () =>
    {
        try { await _nav.NavigateToCameraAsync(); }
        catch (Exception ex) { _logger.LogError(ex, "Navigation to camera failed"); }
    });

    public ICommand CompleteProcedureCommand => new RelayCommand(async () =>
    {
        try { await _nav.ShowProcedureChecklistAsync(); }
        catch (Exception ex) { _logger.LogError(ex, "Failed to open procedure checklist"); }
    });

    public ICommand OpenInsightsCommand => new RelayCommand<AIReport>(async report =>
    {
        if (report is null) return;
        try { await _nav.ShowInsightsAsync(report); }
        catch (Exception ex) { _logger.LogError(ex, "Failed to open insights sheet"); }
    });

    // Команда открытия деталей ограничения
    [RelayCommand]
    private async Task OpenRestrictionDetailsAsync(RestrictionTimer? timer)
    {
        if (timer is null) return;
        try { await _nav.ShowRestrictionDetailsAsync(timer); }
        catch (Exception ex) { _logger.LogError(ex, "Failed to open restriction details"); }
    }

    // Команда показа всех ограничений
    [RelayCommand]
    private async Task ShowAllRestrictionsAsync()
    {
        try { await _nav.ShowAllRestrictionsAsync(RestrictionTimers.ToList()); }
        catch (Exception ex) { _logger.LogError(ex, "Failed to show all restrictions"); }
    }

    // Команда показа полного описания (Read more)
    [RelayCommand]
    private async Task ShowDescriptionAsync(ProgressFeedItem? item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.Description)) return;
        try { await _nav.ShowDescriptionAsync(item.Description!); }
        catch (Exception ex) { _logger.LogError(ex, "Failed to show description sheet"); }
    }

    // Команда предварительного просмотра фото на полный экран
    [RelayCommand]
    private async Task PreviewPhotoAsync(ProgressPhoto? photo)
    {
        if (photo is null) return;
        try { await _nav.PreviewPhotoAsync(photo.LocalPath); }
        catch (Exception ex) { _logger.LogError(ex, "Failed to preview photo"); }
    }

    private void BuildVisibleRestrictions()
    {
        VisibleRestrictionItems.Clear();

        // Ensure deterministic ordering – shortest remaining first (ближайшие слева)
        var ordered = RestrictionTimers
            .OrderBy(t => t.DaysRemaining)
            .ToList();

        foreach (var timer in ordered)
            VisibleRestrictionItems.Add(timer);
    }

    private async Task LoadAsync()
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var from = today.AddDays(-(DefaultDaysRange - 1)); // inclusive
            var feedItems = await _queryBus.SendAsync(new Application.Queries.GetProgressFeedQuery(from, today));
            _logger.LogInformation("Fetched {Count} feed items.", feedItems.Count());
            
            Feed.Clear();
            // Требование: показывать только дни, где есть хотя бы одна фотография
            var visible = feedItems.Where(f => f.Photos?.Any() == true)
                                   .OrderByDescending(f => f.Date);

            foreach (var item in visible)
            {
                Feed.Add(item);
            }

            // var todayItem = Feed.FirstOrDefault(); // This line doesn't seem to be used, can be removed if not needed later.
            
            // Load active restrictions via CQRS
            RestrictionTimers.Clear();
            var restrictions = await _queryBus.SendAsync(new Application.Queries.GetRestrictionsQuery());
            foreach (var t in restrictions)
                RestrictionTimers.Add(t);

            BuildVisibleRestrictions();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading progress feed");
        }
    }

    // Pull-to-refresh binding
    [RelayCommand]
    private Task Load() => LoadAsync();

    // При получении нового фото освежаем ленту (без блокировки UI)
    public void Receive(PhotoCapturedMessage message)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var existing = Feed.OfType<ProgressFeedItem>().FirstOrDefault(f => f.Date == today);
        if (existing == null)
        {
            existing = new ProgressFeedItem(
                Date: today,
                Title: $"Day {(_profileService.SurgeryDate == DateTime.Today ? 1 : (DateTime.Today - _profileService.SurgeryDate).Days + 1)}",
                Description: string.Empty,
                Photos: new List<ProgressPhoto>(),
                ActiveRestrictions: new List<RestrictionTimer>(),
                AiReport: null)
            {
                DoctorReportSummary = string.Empty
            };
            Feed.Insert(0, existing);
        }

        var photoList = existing.Photos as List<ProgressPhoto> ?? new List<ProgressPhoto>(existing.Photos);
        photoList.Add(new ProgressPhoto
        {
            LocalPath = message.Value,
            CapturedAt = DateTime.Now,
            Zone = PhotoZone.Front,
            AiScore = 0
        });
        // recreate item with updated list to notify UI
        existing = existing with { Photos = photoList };
        Feed[Feed.IndexOf(Feed.First(f => f.Date == today))] = existing;
    }

    // При изменении ограничений обновляем только верхнюю полосу
    public void Receive(RestrictionsChangedMessage message)
    {
        _ = ReloadRestrictionsAsync();
    }

    private async Task ReloadRestrictionsAsync()
    {
        try
        {
            RestrictionTimers.Clear();
            var restrictions = await _queryBus.SendAsync(new Application.Queries.GetRestrictionsQuery());
            foreach (var t in restrictions)
                RestrictionTimers.Add(t);

            BuildVisibleRestrictions();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading restrictions");
        }
    }
} 