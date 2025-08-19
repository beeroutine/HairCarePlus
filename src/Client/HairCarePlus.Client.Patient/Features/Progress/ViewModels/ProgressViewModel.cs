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
using MauiApp = Microsoft.Maui.Controls.Application;
using HairCarePlus.Client.Patient.Features.Progress.Selectors;
using CommunityToolkit.Mvvm.Messaging;
using HairCarePlus.Client.Patient.Features.PhotoCapture.Application.Messages;
using HairCarePlus.Client.Patient.Features.PhotoCapture.Views;
using HairCarePlus.Client.Patient.Features.Progress.Application.Messages;
using HairCarePlus.Client.Patient.Infrastructure.Services.Interfaces;
using HairCarePlus.Client.Patient.Features.Progress.Services.Interfaces;
using HairCarePlus.Client.Patient.Features.Sync.Messages;
using System.Threading; // add for SemaphoreSlim
using System.Collections.Generic;
using Microsoft.Maui.ApplicationModel; // MainThread
using CommunityToolkit.Maui.Extensions;

namespace HairCarePlus.Client.Patient.Features.Progress.ViewModels;

public partial class ProgressViewModel : ObservableObject, IRecipient<PhotoSavedMessage>, IRecipient<RestrictionsChangedMessage>, IRecipient<PhotoReportSyncedMessage>, IRecipient<HairCarePlus.Client.Patient.Features.Sync.Messages.PhotoCommentSyncedMessage>
{
    private readonly IQueryBus _queryBus;
    private readonly ILogger<ProgressViewModel> _logger;
    private readonly IServiceProvider _sp;
    private readonly IProfileService _profileService;
    private readonly IProgressNavigationService _nav;

    // Map date -> feed item for quick incremental updates
    private readonly Dictionary<DateOnly, ProgressFeedItem> _feedByDate = new();

    // Helper for building absolute path
    private static string BuildMediaPath(string fileName)
        => System.IO.Path.Combine(FileSystem.AppDataDirectory, "Media", fileName);

    // Prevent parallel executions of LoadAsync which cause CollectionView inconsistency
    private readonly SemaphoreSlim _loadLock = new(1, 1);

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
        _feed = new ObservableCollection<ProgressFeedItem>();
        VisibleRestrictionItems = new ObservableCollection<object>();

        _ = LoadAsync();

        // подписка на сообщение о новом фото
        WeakReferenceMessenger.Default.Register<PhotoSavedMessage>(this);
        // подписка на изменение ограничений
        WeakReferenceMessenger.Default.Register<RestrictionsChangedMessage>(this);
        // подписка на синхронизированный с сервера фото-отчёт
        WeakReferenceMessenger.Default.Register<PhotoReportSyncedMessage>(this);
        // подписка на синхронизированный комментарий к фото
        WeakReferenceMessenger.Default.Register<PhotoCommentSyncedMessage>(this);
    }

    public ObservableCollection<RestrictionTimer> RestrictionTimers { get; }
    [ObservableProperty]
    private ObservableCollection<ProgressFeedItem> _feed;
    public ObservableCollection<object> VisibleRestrictionItems { get; }

    /// <summary>
    /// Дата операции пациента (для годового таймлайна).
    /// TODO: загрузить из профиля.
    /// </summary>
    public DateTime SurgeryDate => _profileService.SurgeryDate;

    // Selected feed item (for future insights)
    [ObservableProperty]
    private ProgressFeedItem? _selectedFeedItem;

    [ObservableProperty]
    private bool isRefreshing;

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

    // Команда открытия деталей ограничения через Stories popup
    [RelayCommand]
    private async Task OpenRestrictionDetailsAsync(RestrictionTimer? timer)
    {
        if (timer is null) return;
        
        try
        {
            var popup = new RestrictionStoriesPopup(timer);
            await PopupExtensions.ShowPopupAsync(MauiApp.Current.MainPage, popup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open restriction details popup");
        }
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
        // fast-fail if a load is already running
        if (!await _loadLock.WaitAsync(0))
            return;

        IsRefreshing = true;
        try
        {
            var feedItems = await _queryBus.SendAsync(new Application.Queries.GetLocalPhotoReportsQuery());
            _logger.LogInformation("Fetched {Count} feed items from local database.", feedItems.Count());
            
            // Fill dictionary for incremental updates
            _feedByDate.Clear();
            foreach (var fi in feedItems)
                _feedByDate[fi.Date] = fi;

            // Atomically replace collection to avoid UI inconsistencies
            Feed = new ObservableCollection<ProgressFeedItem>(feedItems);

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
        finally
        {
            IsRefreshing = false;
            _loadLock.Release();
        }
    }

    // Pull-to-refresh binding
    [RelayCommand]
    private Task Load() => LoadAsync();

    // При получении нового фото просто полностью перезагружаем ленту из БД
    public void Receive(PhotoSavedMessage message)
    {
        try
        {
            var date = DateOnly.FromDateTime(DateTime.Now);

            if (!_feedByDate.TryGetValue(date, out var feedItem))
            {
                feedItem = new ProgressFeedItem(
                    Date: date,
                    Title: "Фотоотчёт",
                    Description: string.Empty,
                    Photos: new List<ProgressPhoto>(),
                    ActiveRestrictions: new List<RestrictionTimer>());

                _feedByDate[date] = feedItem;
                MainThread.BeginInvokeOnMainThread(() => Feed.Insert(0, feedItem));
            }

            var newPhotos = feedItem.Photos
                .Append(new ProgressPhoto
                {
                    LocalPath = BuildMediaPath(message.Value),
                    CapturedAt = DateTime.Now,
                    Zone = PhotoZone.Front
                })
                .ToList();

            var refreshed = feedItem with { Photos = newPhotos };

            _feedByDate[date] = refreshed;

            var idx = Feed.IndexOf(feedItem);
            if (idx >= 0)
            {
                MainThread.BeginInvokeOnMainThread(() => Feed[idx] = refreshed);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed incremental UI update on PhotoSavedMessage");
        }
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

    // При приходе нового/обновлённого PhotoReport с сервера перезагружаем ленту
    public void Receive(PhotoReportSyncedMessage message)
    {
        // не блокируем UI, просто запустим фоновое обновление
        _ = LoadAsync();
    }

    // При приходе нового комментария от клиники обновляем соответствующий день
    public void Receive(PhotoCommentSyncedMessage msg)
    {
        // Locate feed item by report id instead of date to avoid mismatches with time zones
        var reportId = msg.Comment.PhotoReportId;
        // Find the item that contains this photo report
        var item = Feed.FirstOrDefault(f => f.Photos.Any(p => string.Equals(p.ReportId, reportId, StringComparison.OrdinalIgnoreCase)));
        if (item == null) return;

        var index = Feed.IndexOf(item);
        if (index != -1)
        {
            var updated = item with { DoctorReportSummary = msg.Comment.Text };
            Feed[index] = updated;
        }
    }
} 