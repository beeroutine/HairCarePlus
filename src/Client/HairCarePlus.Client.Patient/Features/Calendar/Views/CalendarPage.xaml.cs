using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;

namespace HairCarePlus.Client.Patient.Features.Calendar.Views
{
    public partial class CalendarPage : ContentPage
    {
        public CalendarPage(CalendarViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
} 