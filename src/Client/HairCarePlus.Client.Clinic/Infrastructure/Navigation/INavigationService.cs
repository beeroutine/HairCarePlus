using System.Threading.Tasks;

namespace HairCarePlus.Client.Clinic.Infrastructure.Navigation;

public interface INavigationService
{
    Task GoBackAsync();
    Task NavigateToAsync(string route);
}