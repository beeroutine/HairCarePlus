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

namespace HairCarePlus.Client.Patient.Features.Progress.ViewModels;

public partial class ProgressViewModel : ObservableObject
{
    private readonly IQueryBus _queryBus;
    private readonly ILogger<ProgressViewModel> _logger;
    private readonly IServiceProvider _sp;

    private const int DefaultDaysRange = 7;
    private const int MaxVisibleRestrictionItems = 4;

    public ProgressViewModel(IQueryBus queryBus, ILogger<ProgressViewModel> logger, IServiceProvider sp)
    {
        _queryBus = queryBus;
        _logger = logger;
        _sp = sp;

        RestrictionTimers = new ObservableCollection<RestrictionTimer>();
        Feed = new ObservableCollection<ProgressFeedItem>();
        VisibleRestrictionItems = new ObservableCollection<object>();

        _ = LoadAsync();
    }

    public ObservableCollection<RestrictionTimer> RestrictionTimers { get; }
    public ObservableCollection<ProgressFeedItem> Feed { get; }
    public ObservableCollection<object> VisibleRestrictionItems { get; }

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

    // Команда открытия деталей ограничения
    [RelayCommand]
    private void OpenRestrictionDetails(RestrictionTimer? timer)
    {
        if (timer is null) return;
        try
        {
            var popup = new RestrictionDetailPopup(timer);
            Shell.Current.CurrentPage?.ShowPopup(popup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open restriction details");
        }
    }

    // Команда показа всех ограничений
    [RelayCommand]
    private void ShowAllRestrictions()
    {
        try
        {
            var popup = new AllRestrictionsPopup(RestrictionTimers.ToList());
            Shell.Current.CurrentPage?.ShowPopup(popup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show all restrictions");
        }
    }

    // Команда показа полного описания (Read more)
    [RelayCommand]
    private void ShowDescription(ProgressFeedItem? item)
    {
        if (item is null) return;

        try
        {
            // For now, display a simple popup with the full description.
            var popup = new CommunityToolkit.Maui.Views.Popup
            {
                Content = new Label
                {
                    Text = item.Description,
                    Padding = new Thickness(20),
                    FontSize = 14,
                    LineBreakMode = LineBreakMode.WordWrap
                }
            };

            Shell.Current.CurrentPage?.ShowPopup(popup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show description popup");
        }
    }

    private void BuildVisibleRestrictions()
    {
        VisibleRestrictionItems.Clear();
        if (RestrictionTimers.Count == 0) return;

        if (RestrictionTimers.Count <= MaxVisibleRestrictionItems)
        {
            foreach (var t in RestrictionTimers)
                VisibleRestrictionItems.Add(t);
        }
        else
        {
            for (int i = 0; i < MaxVisibleRestrictionItems - 1; i++)
                VisibleRestrictionItems.Add(RestrictionTimers[i]);
            VisibleRestrictionItems.Add(new ShowMoreRestrictionPlaceholderViewModel
            {
                CountLabel = $"+{RestrictionTimers.Count - (MaxVisibleRestrictionItems - 1)}"
            });
        }
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
            // Counter to help vary dummy data
            int counter = 0;
            foreach (var originalItem in feedItems.OrderByDescending(f => f.Date)) // Ensure latest is first for dummy data assignment
            {
                string currentDescription;
                string currentDoctorReportSummary;
                AIReport? currentAiReport = originalItem.AiReport; // Start with original or null

                if (counter == 0) // First item (most recent)
                {
                    currentDescription = "This is a recent update with a moderate amount of text. We're checking to see how the layout handles this. Swelling has reduced significantly today.";
                    currentDoctorReportSummary = "Looks good. Continue protocol. Next check-up in 3 days.";
                    if (currentAiReport == null && originalItem.Photos.Any()) 
                        currentAiReport = new AIReport(originalItem.Date, 75, "AI: Healing progressing well.");
                }
                else if (counter == 1)
                {
                    currentDescription = "A very short note about yesterday's status. Seems okay.";
                    currentDoctorReportSummary = "Minor redness noted. Monitor closely and report any changes.";
                    if (currentAiReport == null && originalItem.Photos.Any()) 
                        currentAiReport = new AIReport(originalItem.Date, 60, "AI: Slight inflammation detected. Pay attention.");
                }
                else
                {
                    currentDescription = "This is a much longer description designed to test the MaxLines and TailTruncation feature. We need to ensure that the text wraps correctly and the 'Read More' functionality (to be implemented) will have enough content to expand. The patient reports slight itching in the donor area, which is normal at this stage. Overall progress seems to be on track according to the established recovery timeline. No signs of infection observed. Vitamin supplements were taken as prescribed.";
                    currentDoctorReportSummary = "Patient reports itching, which is common. Advised to use the prescribed spray. If symptoms persist or worsen, schedule an immediate follow-up. Otherwise, everything appears to be developing as expected for this phase of recovery. Continue current care regimen.";
                    if (currentAiReport == null && originalItem.Photos.Any()) 
                        currentAiReport = new AIReport(originalItem.Date, 85, "AI: Itching reported - this is a normal healing symptom. Overall progress is positive.");
                }
                
                // Fallback to ensure AiReport has some value if photos exist and AiReport is still null, for UI binding consistency
                if (currentAiReport == null && originalItem.Photos.Any())
                {
                    currentAiReport = new AIReport(originalItem.Date, (counter % 3 + 1) * 25, $"AI Default Score: {(counter % 3 + 1) * 25}");
                }

                var modifiedItem = originalItem with
                {
                    Description = currentDescription,
                    DoctorReportSummary = currentDoctorReportSummary,
                    AiReport = currentAiReport
                };
                
                Feed.Add(modifiedItem);
                counter++;
            }

            // var todayItem = Feed.FirstOrDefault(); // This line doesn't seem to be used, can be removed if not needed later.
            
            RestrictionTimers.Clear();
            var dummyRestrictions = new List<RestrictionTimer>
            {
                new RestrictionTimer { Title = "Окрашивание волос и стайлинг", DaysRemaining = 243 },
                new RestrictionTimer { Title = "Первая стрижка машинкой очень коротко", DaysRemaining = 58 },
                new RestrictionTimer { Title = "Интенсивный загар и солярий", DaysRemaining = 28 },
                new RestrictionTimer { Title = "Активный спорт и нагрузки", DaysRemaining = 28 },
                new RestrictionTimer { Title = "Бассейн и сауна", DaysRemaining = 20 },
                new RestrictionTimer { Title = "Крепкий алкоголь", DaysRemaining = 15 },
                new RestrictionTimer { Title = "Ношение тесных шапок", DaysRemaining = 10 },
                new RestrictionTimer { Title = "Сон на животе", DaysRemaining = 5 }
            };
            foreach (var t in dummyRestrictions)
                 RestrictionTimers.Add(t);

            BuildVisibleRestrictions();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading progress feed");
        }
    }
} 