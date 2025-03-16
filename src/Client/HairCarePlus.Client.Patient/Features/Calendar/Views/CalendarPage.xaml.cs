using System;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Xaml;

namespace HairCarePlus.Client.Patient.Features.Calendar.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CalendarPage : ContentPage
    {
        public CalendarPage(IServiceProvider serviceProvider)
        {
            // We can't use InitializeComponent() since the XAML file might not be fully implemented yet
            // Instead, use code-behind to create the UI
            
            // Create the ViewModel and inject services
            var calendarService = serviceProvider.GetRequiredService<ICalendarService>();
            var notificationService = serviceProvider.GetRequiredService<INotificationService>();
            
            BindingContext = new CalendarViewModel(calendarService, notificationService);
            
            // This is a temporary implementation until we complete the custom XAML initialization
            Content = new ScrollView
            {
                Content = new StackLayout
                {
                    Padding = new Thickness(16),
                    Spacing = 20,
                    Children =
                    {
                        new Label
                        {
                            Text = "Calendar",
                            FontSize = 24,
                            FontAttributes = FontAttributes.Bold,
                            HorizontalOptions = LayoutOptions.Start
                        },
                        new RestrictionTimersView 
                        { 
                            BindingContext = BindingContext
                        },
                        new HorizontalCalendarView
                        {
                            BindingContext = BindingContext
                        },
                        new MonthCalendarView
                        {
                            BindingContext = BindingContext
                        }
                    }
                }
            };
        }
        
        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // Refresh data when page appears
            if (BindingContext is CalendarViewModel viewModel)
            {
                // Trigger data refresh
                viewModel.RefreshCommand?.Execute(null);
            }
        }
    }
} 