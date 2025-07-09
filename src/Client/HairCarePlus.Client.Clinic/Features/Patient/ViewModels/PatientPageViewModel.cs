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

namespace HairCarePlus.Client.Clinic.Features.Patient.ViewModels;

public partial class PatientPageViewModel : ObservableObject, IQueryAttributable
{
    private readonly IPhotoReportService _photoReportService;
    private readonly HairCarePlus.Client.Clinic.Infrastructure.Features.Patient.IPatientService _patientService;
    private readonly HairCarePlus.Client.Clinic.Infrastructure.Features.Patient.IRestrictionService _restrictionService;
    private readonly ILogger<PatientPageViewModel> _logger;

    // Simple DTOs
    public record RestrictionTimer(HairCarePlus.Shared.Domain.Restrictions.RestrictionIconType IconType, int DaysRemaining, double Progress);
    public partial class PhotoEntry : ObservableObject
    {
        // Unique identifier of a photo report (could be guid from server)
        public string Id { get; }

        public string ImageUrl { get; }

        [ObservableProperty]
        private string? _comment;

        // Draft text that doctor types before sending.
        [ObservableProperty]
        private string _commentDraft = string.Empty;

        public IAsyncRelayCommand SendCommentCommand { get; }

        private readonly PatientPageViewModel _parent;

        public PhotoEntry(string id, string imageUrl, string? comment, PatientPageViewModel parent)
        {
            Id = id;
            ImageUrl = imageUrl;
            _comment = comment;
            _parent = parent;

            SendCommentCommand = new AsyncRelayCommand(SendAsync);
        }

        private async Task SendAsync()
        {
            if (string.IsNullOrWhiteSpace(CommentDraft))
                return;

            var text = CommentDraft.Trim();

            // Optimistically update UI
            Comment = text;
            CommentDraft = string.Empty;

            try
            {
                await _parent.SubmitCommentAsync(Id, text);
            }
            catch
            {
                // TODO: Add error handling / revert on failure
            }
        }
    }

    [ObservableProperty] private string _patientId = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _avatarUrl;
    [ObservableProperty] private double _dayProgress;

    public ObservableCollection<RestrictionTimer> Restrictions { get; } = new();
    public ObservableCollection<PhotoEntry> Feed { get; } = new();

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

        // Load photo reports
        Feed.Clear();
        var reports = await _photoReportService.GetReportsAsync(PatientId);
        foreach (var rep in reports)
            Feed.Add(new PhotoEntry(rep.Id.ToString(), rep.ImageUrl, rep.Notes, this));

        // Subscribe to real-time updates
        await _photoReportService.ConnectAsync(PatientId);
    }

    // Submit comment to server (stub for now)
    public async Task SubmitCommentAsync(string photoReportId, string comment)
    {
        const string demoDoctorId = "doctor-1"; // TODO: pull from auth context
        var result = await _photoReportService.AddCommentAsync(PatientId, photoReportId, demoDoctorId, comment);
        // nothing else: optimistic UI already updated; server will push to patient app
    }
} 