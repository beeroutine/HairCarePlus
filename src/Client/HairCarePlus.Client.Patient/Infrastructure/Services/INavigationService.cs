namespace HairCarePlus.Client.Patient.Infrastructure.Services
{
    public interface INavigationService
    {
        Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null);
        Task NavigateBackAsync();
        Task NavigateToMainAsync();
        Task NavigateToLoginAsync();
    }
} 