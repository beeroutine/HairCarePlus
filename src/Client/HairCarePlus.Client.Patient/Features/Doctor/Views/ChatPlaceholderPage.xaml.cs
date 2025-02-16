using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Doctor.Views
{
    public partial class ChatPlaceholderPage : ContentPage
    {
        private bool _isNavigating;

        public ChatPlaceholderPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            if (!_isNavigating)
            {
                _isNavigating = true;
                try
                {
                    await Shell.Current.GoToAsync("//chat");
                }
                catch
                {
                    _isNavigating = false;
                }
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _isNavigating = false;
        }
    }
} 