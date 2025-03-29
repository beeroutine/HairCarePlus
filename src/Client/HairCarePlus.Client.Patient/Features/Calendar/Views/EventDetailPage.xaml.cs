using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;

namespace HairCarePlus.Client.Patient.Features.Calendar.Views
{
    [QueryProperty(nameof(EventId), "id")]
    public partial class EventDetailPage : ContentPage
    {
        private readonly EventDetailViewModel _viewModel;

        public string EventId
        {
            set
            {
                if (int.TryParse(value, out int id))
                {
                    _ = _viewModel.LoadEventAsync(id);
                }
            }
        }

        public EventDetailPage(EventDetailViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }
    }
} 