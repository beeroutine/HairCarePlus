using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Client.Clinic.Infrastructure.Features.Progress;
using System.Linq;
using HairCarePlus.Client.Clinic.Infrastructure.Features.Patient;
using Microsoft.Maui.Controls;
using HairCarePlus.Shared.Communication;
using Microsoft.Extensions.Logging;
using HairCarePlus.Client.Clinic.Features.Patient.Models;

namespace HairCarePlus.Client.Clinic.Features.Patient.ViewModels;

public partial class PatientPageViewModel : ObservableObject, IQueryAttributable
{
    private readonly IPhotoReportService _photoReportService;
    private readonly HairCarePlus.Client.Clinic.Infrastructure.Features.Patient.IPatientService _patientService;
    private readonly HairCarePlus.Client.Clinic.Infrastructure.Features.Patient.IRestrictionService _restrictionService;
    private readonly ILogger<PatientPageViewModel> _logger;

    // Simple DTOs
    public record RestrictionTimer(HairCarePlus.Shared.Domain.Restrictions.RestrictionIconType IconType, int DaysRemaining, double Progress);
    // Progress feed item models live in separate namespace for reuse

    [ObservableProperty] private string _patientId = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _avatarUrl;
    [ObservableProperty] private double _dayProgress;
    [ObservableProperty]
    private bool _isRefreshing;

    // Items displayed in UI (RestrictionTimer or ShowMore placeholder)
    public ObservableCollection<object> VisibleRestrictionItems { get; } = new();

    private const int MaxVisibleRestrictions = 8;

    public ObservableCollection<RestrictionTimer> Restrictions { get; } = new();
    public ObservableCollection<ProgressFeedItem> Feed { get; } = new();

    // Avoid multiple event subscriptions during repeated LoadAsync calls
    private bool _subscribed;

    public AsyncRelayCommand LoadCommand { get; }
    public IRelayCommand OpenChatCommand { get; }
    public IRelayCommand OpenProgressCommand { get; }
    public AsyncRelayCommand<ProgressFeedItem?> StartCommentCommand { get; }
    public IRelayCommand ShowAllRestrictionsCommand { get; }

    // Inline Instagram-like comment bar state
    [ObservableProperty] private bool _isCommenting;
    [ObservableProperty] private string _commentText = string.Empty;
    [ObservableProperty] private ProgressFeedItem? _commentTarget;
    public AsyncRelayCommand SendCommentCommand { get; }
    public IRelayCommand CancelCommentCommand { get; }
    public bool IsSendEnabled => CommentTarget != null && !string.IsNullOrWhiteSpace(CommentText);

    partial void OnIsCommentingChanged(bool oldValue, bool newValue)
    {
        _logger.LogInformation("IsCommenting changed: {Old} -> {New}", oldValue, newValue);
    }

    partial void OnCommentTextChanged(string value)
    {
        OnPropertyChanged(nameof(IsSendEnabled));
    }

    partial void OnCommentTargetChanged(ProgressFeedItem? value)
    {
        OnPropertyChanged(nameof(IsSendEnabled));
    }

    public PatientPageViewModel(IPhotoReportService photoReportService,
        HairCarePlus.Client.Clinic.Infrastructure.Features.Patient.IPatientService patientService,
        HairCarePlus.Client.Clinic.Infrastructure.Features.Patient.IRestrictionService restrictionService,
        ILogger<PatientPageViewModel> logger)
    {
        _photoReportService = photoReportService;
        _patientService = patientService;
        _restrictionService = restrictionService;
        _logger = logger;
        LoadCommand = new AsyncRelayCommand(LoadAsync);

        OpenChatCommand = new RelayCommand(async () =>
        {
            if (string.IsNullOrEmpty(PatientId)) return;
            await Shell.Current.GoToAsync(nameof(HairCarePlus.Client.Clinic.Features.Chat.Views.ChatPage), true, new Dictionary<string, object>
            {
                { "patientId", PatientId }
            });
        });

        OpenProgressCommand = new RelayCommand(async () =>
        {
            if (string.IsNullOrEmpty(PatientId)) return;
            await Shell.Current.GoToAsync("patient-progress", true, new Dictionary<string, object>
            {
                { "patientId", PatientId }
            });
        });

        StartCommentCommand = new AsyncRelayCommand<ProgressFeedItem?>(StartCommentAsync);

        SendCommentCommand = new AsyncRelayCommand(async () =>
        {
            if (CommentTarget == null) return;
            var text = CommentText?.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            // Choose a photo with ReportId to attach the comment
            var photoWithId = CommentTarget.Photos.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.ReportId));
            if (photoWithId == null) return; // wait until feed item has a stable ReportId
            // Optimistic UI: update local DB and outbox first; never block on network
            try
            {
                await SubmitCommentAsync(photoWithId.ReportId!, text);
            }
            catch (Exception ex)
            {
                // Even если локальная запись не удалась, не ломаем UX: просто логируем
                _logger.LogError(ex, "Failed to submit comment locally for report {ReportId}", photoWithId.ReportId);
            }

            // Update local feed/UI state
            var index = Feed.IndexOf(CommentTarget);
            if (index >= 0)
            {
                var updated = CommentTarget with { DoctorReportSummary = text };
                Feed[index] = updated;
            }

            // Fire-and-forget background sync, UI уже обновлён локально
            try { _ = Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(async () => await _photoReportService.ConnectAsync(PatientId)); } catch { }
            CommentText = string.Empty;
            CommentTarget = null;
            IsCommenting = false;
            OnPropertyChanged(nameof(IsSendEnabled));
        });

        CancelCommentCommand = new RelayCommand(() =>
        {
            CommentText = string.Empty;
            CommentTarget = null;
            IsCommenting = false;
            OnPropertyChanged(nameof(IsSendEnabled));
        });

        ShowAllRestrictionsCommand = new RelayCommand(() =>
        {
            VisibleRestrictionItems.Clear();
            foreach (var item in Restrictions)
                VisibleRestrictionItems.Add(item);
        });
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("patientId", out var idObj) && idObj is string id)
        {
            PatientId = id;
            _ = LoadCommand.ExecuteAsync(null);
        }
    }

    private async Task LoadAsync()
    {
        IsRefreshing = true;
        // Ensure we are connected to events BEFORE first load so cache is fresh and we catch first set
        await _photoReportService.ConnectAsync(PatientId);
        // Fetch summary from API for demo
        var summaries = await _patientService.GetPatientsAsync();
        var summary = summaries.FirstOrDefault(p => p.Id == PatientId);

        Name = summary?.Name ?? "Пациент";
        DayProgress = summary?.DayProgress ?? 0;
        AvatarUrl = summary?.AvatarUrl;

        // Load restrictions
        Restrictions.Clear();
        var restrictionDtos = await _restrictionService.GetRestrictionsAsync(PatientId);
        _logger.LogInformation("PatientPageViewModel loaded {Count} restrictions", restrictionDtos.Count);
        foreach (var r in restrictionDtos)
        {
            _logger.LogInformation("Adding timer: IconType={IconType}, DaysRemaining={Days}, Progress={Progress:p1}", r.IconType, r.DaysRemaining, r.Progress);
            Restrictions.Add(new RestrictionTimer(r.IconType, r.DaysRemaining, r.Progress));
        }

        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(UpdateVisibleRestrictionItems);

        // Load photo reports and convert to grouped feed items
        Feed.Clear();
        var reports = await _photoReportService.GetReportsAsync(PatientId);

        var earliestDate = reports.Any() ? DateOnly.FromDateTime(reports.Min(r => r.Date)) : (DateOnly?)null;

        // Prefer grouping by atomic set when available; fallback to date grouping
        if (reports.Any(r => r.SetId.HasValue && r.SetId.Value != Guid.Empty))
        {
            var groupedBySet = reports.GroupBy(r => r.SetId!.Value).OrderByDescending(g => g.First().Date);
            foreach (var group in groupedBySet)
            {
                var items = group.ToList();
                var first = items.First();
                var groupDate = DateOnly.FromDateTime(first.Date);
                var photos = items.Select(r => new ProgressPhoto
                {
                    ReportId = r.Id.ToString(),
                    ImageUrl = r.ImageUrl,
                    LocalPath = r.LocalPath,
                    CapturedAt = r.Date,
                    Zone = r.Type switch
                    {
                        PhotoType.FrontView => PhotoZone.Front,
                        PhotoType.TopView => PhotoZone.Top,
                        PhotoType.BackView => PhotoZone.Back,
                        _ => PhotoZone.Front
                    }
                }).ToList();

                var doctorNote = items.Select(r => r.Notes).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
                var dayIndex = earliestDate.HasValue ? (groupDate.DayNumber - earliestDate.Value.DayNumber) + 1 : 1;
                var aiPlaceholder = new AIReport(groupDate, 0, "_Awaiting official AI analysis. Auto-generated score for preview purposes._");
                var item = new ProgressFeedItem(
                    Date: groupDate,
                    Title: $"Day {dayIndex}",
                    Description: null,
                    Photos: photos,
                    ActiveRestrictions: new List<string>(),
                    DoctorReportSummary: doctorNote,
                    AiReport: aiPlaceholder
                );
                Feed.Add(item);
            }
        }
        else
        {
            var groupedByDate = reports.GroupBy(r => DateOnly.FromDateTime(r.Date)).OrderByDescending(g => g.Key);
            foreach (var group in groupedByDate)
            {
                var items = group.ToList();
            var first = items.First();
            var groupDate = DateOnly.FromDateTime(first.Date);
            var photos = items.Select(r => new ProgressPhoto
            {
                ReportId = r.Id.ToString(),
                ImageUrl = r.ImageUrl,
                LocalPath = r.LocalPath,
                CapturedAt = r.Date,
                Zone = r.Type switch
                {
                    PhotoType.FrontView => PhotoZone.Front,
                    PhotoType.TopView => PhotoZone.Top,
                    PhotoType.BackView => PhotoZone.Back,
                    _ => PhotoZone.Front
                }
            }).ToList();

                var doctorNote = items.Select(r => r.Notes).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
                var dayIndex = earliestDate.HasValue ? (groupDate.DayNumber - earliestDate.Value.DayNumber) + 1 : 1;
                var aiPlaceholder = new AIReport(groupDate, 0, "_Awaiting official AI analysis. Auto-generated score for preview purposes._");
                var item = new ProgressFeedItem(
                    Date: groupDate,
                    Title: $"Day {dayIndex}",
                    Description: null,
                    Photos: photos,
                    ActiveRestrictions: new List<string>(),
                    DoctorReportSummary: doctorNote,
                    AiReport: aiPlaceholder
                );
                Feed.Add(item);
            }
        }

        // Subscribe to real-time updates and refresh feed when new set arrives
        if (!_subscribed)
        {
            _subscribed = true;
            HairCarePlus.Client.Clinic.Infrastructure.Network.Events.IEventsSubscription? ev = null;
            try
            {
                // pull events subscription from service private field via reflection – keeps public surface minimal
                var svcType = _photoReportService.GetType();
                var field = svcType.GetField("_events", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                ev = field?.GetValue(_photoReportService) as HairCarePlus.Client.Clinic.Infrastructure.Network.Events.IEventsSubscription;
            }
            catch { }
            if (ev != null)
            {
                ev.PhotoReportSetAdded += async (_, __) =>
                {
                    try
                    {
                        // re-load feed after cache invalidation to immediately show new photos
                        await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await LoadAsync();
                        });
                    }
                    catch { }
                };

                ev.RestrictionChangedEvent += async (_, dto) =>
                {
                    if (dto != null && dto.PatientId.ToString() == PatientId)
                    {
                        try
                        {
                            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                await LoadAsync();
                            });
                        }
                        catch { }
                    }
                };

                ev.CalendarTaskChangedEvent += async (_, dto) =>
                {
                    if (dto != null && dto.PatientId.ToString() == PatientId)
                    {
                        try
                        {
                            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                await LoadAsync();
                            });
                        }
                        catch { }
                    }
                };
            }
        }

        IsRefreshing = false;
    }

    private void UpdateVisibleRestrictionItems()
    {
        VisibleRestrictionItems.Clear();

        if (Restrictions.Count <= MaxVisibleRestrictions)
        {
            foreach (var item in Restrictions)
                VisibleRestrictionItems.Add(item);
        }
        else
        {
            int take = MaxVisibleRestrictions - 1;
            foreach (var item in Restrictions.Take(take))
                VisibleRestrictionItems.Add(item);

            int remaining = Restrictions.Count - take;
            VisibleRestrictionItems.Add(new ShowMoreRestrictionPlaceholderViewModel { CountLabel = $"+{remaining}" });
        }
    }

    private async Task StartCommentAsync(ProgressFeedItem? item)
    {
        if (item == null) return;
        _logger.LogInformation("StartComment tapped for date {Date}, photos={Count}", item.Date, item.Photos.Count);
        CommentTarget = item;
        // Prefill with existing doctor note to enable edit-in-place
        CommentText = item.DoctorReportSummary ?? string.Empty;
        IsCommenting = true;
        OnPropertyChanged(nameof(IsSendEnabled));
    }

    public async Task SubmitCommentAsync(string photoReportId, string comment)
    {
        // no inline editing in new feed; left for future extension
        const string demoDoctorId = "doctor-1";
        await _photoReportService.AddCommentAsync(PatientId, photoReportId, demoDoctorId, comment);
    }
} 