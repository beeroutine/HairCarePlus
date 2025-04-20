using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Infrastructure.Services
{
    public interface INavigationService
    {
        Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null);
        Task NavigateToAsync<T>(IDictionary<string, object>? parameters = null) where T : class;
        Task NavigateBackAsync();
        Task NavigateToMainAsync();
        Task NavigateToLoginAsync();
        Task GoBackAsync();
    }
} 