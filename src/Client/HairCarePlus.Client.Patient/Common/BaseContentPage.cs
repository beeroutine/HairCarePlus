using HairCarePlus.Client.Patient.Infrastructure.Services;

namespace HairCarePlus.Client.Patient.Common
{
    public abstract class BaseContentPage : ContentPage
    {
        protected readonly INavigationService NavigationService;
        protected ViewModelBase ViewModel => BindingContext as ViewModelBase;

        protected BaseContentPage(INavigationService navigationService)
        {
            NavigationService = navigationService;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            SetupBindings();
            SetupSubscriptions();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            CleanupSubscriptions();
        }

        protected virtual void SetupBindings()
        {
            // Override in derived classes to set up bindings
        }

        protected virtual void SetupSubscriptions()
        {
            // Override in derived classes to set up event subscriptions
        }

        protected virtual void CleanupSubscriptions()
        {
            // Override in derived classes to clean up event subscriptions
        }

        protected async Task ShowErrorAsync(string message)
        {
            await DisplayAlert("Error", message, "OK");
        }

        protected async Task ShowSuccessAsync(string message)
        {
            await DisplayAlert("Success", message, "OK");
        }

        protected async Task<bool> ShowConfirmationAsync(string message, string accept = "Yes", string cancel = "No")
        {
            return await DisplayAlert("Confirmation", message, accept, cancel);
        }

        protected async Task ShowLoadingAsync(string message = "Loading...")
        {
            await DisplayAlert("Loading", message, "OK");
        }

        protected void HideLoading()
        {
            // Implement loading indicator hide logic
        }
    }
} 