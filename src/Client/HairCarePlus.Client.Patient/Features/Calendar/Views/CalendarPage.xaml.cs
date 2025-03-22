using System;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Devices;

namespace HairCarePlus.Client.Patient.Features.Calendar.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CalendarPage : ContentPage
    {
        private CalendarViewModel _viewModel;

        public CalendarPage()
        {
            InitializeComponent();
            
            // Получаем сервисы из DI контейнера
            var serviceProvider = IPlatformApplication.Current.Services;
            var calendarService = serviceProvider.GetRequiredService<ICalendarService>();
            var notificationService = serviceProvider.GetRequiredService<INotificationService>();
            
            // Создаем и устанавливаем ViewModel
            _viewModel = new CalendarViewModel(calendarService, notificationService);
            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.RefreshCommand.Execute(null);
        }

        private void OnEventCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.BindingContext is CalendarEvent calendarEvent)
            {
                _viewModel.MarkEventCompletedCommand.Execute(calendarEvent);
            }
        }
    }
} 