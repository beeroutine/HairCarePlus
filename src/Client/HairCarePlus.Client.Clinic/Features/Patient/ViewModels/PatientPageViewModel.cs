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

    public ObservableCollection<RestrictionTimer> Restrictions { get; } = new();
    public ObservableCollection<ProgressFeedItem> Feed { get; } = new();

    public AsyncRelayCommand LoadCommand { get; }
    public IRelayCommand OpenChatCommand { get; }
    public IRelayCommand OpenProgressCommand { get; }

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

        // Load photo reports and convert to feed items
        Feed.Clear();
        var reports = await _photoReportService.GetReportsAsync(PatientId);

        foreach (var rep in reports.OrderByDescending(r => r.Date))
        {
            var item = new ProgressFeedItem(
                Date: DateOnly.FromDateTime(rep.Date),
                Title: rep.Date.ToString("d MMM"),
                Description: null,
                Photos: new List<ProgressPhoto>
                {
                    new ProgressPhoto
                    {
                        ImageUrl = rep.ImageUrl,
                        LocalPath = rep.LocalPath,
                        CapturedAt = rep.Date,
                        Zone = PhotoZone.Front
                    }
                },
                ActiveRestrictions: new List<string>(),
                DoctorReportSummary: string.IsNullOrWhiteSpace(rep.Notes) ? null : rep.Notes
            );
            Feed.Add(item);
        }

        // Subscribe to real-time updates
        await _photoReportService.ConnectAsync(PatientId);
    }

    public async Task SubmitCommentAsync(string photoReportId, string comment)
    {
        // no inline editing in new feed; left for future extension
        const string demoDoctorId = "doctor-1";
        await _photoReportService.AddCommentAsync(PatientId, photoReportId, demoDoctorId, comment);
    }
} 