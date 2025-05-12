using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;
using HairCarePlus.Client.Patient.Infrastructure.Media;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Client.Patient.Features.Progress.ViewModels;

public partial class ProgressViewModel : ObservableObject
{
    private readonly IMediaFileSystemService _fileSystem;
    private readonly ILogger<ProgressViewModel> _logger;

    public ProgressViewModel(IMediaFileSystemService fileSystem, ILogger<ProgressViewModel> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;

        RestrictionTimers = new ObservableCollection<RestrictionTimer>();
        Photos = new ObservableCollection<ProgressPhoto>();

        _ = LoadAsync();
    }

    public ObservableCollection<RestrictionTimer> RestrictionTimers { get; }
    public ObservableCollection<ProgressPhoto> Photos { get; }

    private async Task LoadAsync()
    {
        try
        {
            // 1. Загружаем таймеры ограничений (заглушка)
            RestrictionTimers.Clear();
            RestrictionTimers.Add(new RestrictionTimer { Title = "Alcohol", DaysRemaining = 5 });
            RestrictionTimers.Add(new RestrictionTimer { Title = "Gym", DaysRemaining = 12 });

            // 2. Фото – сканируем локальный кэш
            var dir = await _fileSystem.GetCacheDirectoryAsync();
            var photoDir = Path.Combine(dir, "captured_photos");
            if (!Directory.Exists(photoDir)) return;

            var files = Directory.GetFiles(photoDir, "photo_*.jpg").OrderByDescending(File.GetCreationTimeUtc);
            foreach (var file in files)
            {
                Photos.Add(new ProgressPhoto
                {
                    LocalPath = file,
                    CapturedAt = File.GetCreationTime(file),
                    Zone = GuessZone(file),
                    AiScore = 0
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading progress data");
        }
    }

    private static PhotoZone GuessZone(string path)
    {
        if (path.Contains("front")) return PhotoZone.Front;
        if (path.Contains("top")) return PhotoZone.Top;
        if (path.Contains("back")) return PhotoZone.Back;
        return PhotoZone.Front;
    }
} 