namespace HairCarePlus.Client.Patient.Features.Progress.Services.Interfaces
{
    /// <summary>
    /// Навигационные операции и отображение попапов для функциональности Progress.
    /// </summary>
    public interface IProgressNavigationService
    {
        Task NavigateToCameraAsync();
        Task PreviewPhotoAsync(string localPath);
        Task ShowDescriptionAsync(string description);
        Task ShowRestrictionDetailsAsync(HairCarePlus.Client.Patient.Features.Progress.Domain.Entities.RestrictionTimer timer);
        Task ShowAllRestrictionsAsync(IReadOnlyList<HairCarePlus.Client.Patient.Features.Progress.Domain.Entities.RestrictionTimer> timers);
        Task ShowProcedureChecklistAsync();
        Task ShowInsightsAsync(HairCarePlus.Client.Patient.Features.Progress.Domain.Entities.AIReport report);
    }
} 