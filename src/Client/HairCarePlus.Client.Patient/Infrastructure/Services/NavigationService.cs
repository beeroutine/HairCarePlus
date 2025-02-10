namespace HairCarePlus.Client.Patient.Infrastructure.Services;

public class NavigationService : INavigationService
{
    public async Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        if (parameters != null)
            await Shell.Current.GoToAsync(route, parameters);
        else
            await Shell.Current.GoToAsync(route);
    }

    public async Task NavigateBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    public async Task NavigateToMainAsync()
    {
        await Shell.Current.GoToAsync("//main");
    }

    public async Task NavigateToLoginAsync()
    {
        await Shell.Current.GoToAsync("//login");
    }
} 