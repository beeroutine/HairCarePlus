using System;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace HairCarePlus.Client.Patient.Features.Calendar.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CalendarPage : ContentPage
    {
        private SimpleCalendarViewModel? ViewModel => BindingContext as SimpleCalendarViewModel;

        public CalendarPage()
        {
            try
            {
                InitializeComponent();
                
                // Создаем простую ViewModel без зависимостей
                BindingContext = new SimpleCalendarViewModel();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing CalendarPage: {ex}");
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(async () => {
                    await DisplayAlert("Ошибка", "Произошла ошибка при загрузке календаря. Пожалуйста, попробуйте снова.", "OK");
                });
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            try
            {
                // Принудительно обновляем календарь при появлении страницы
                monthCalendarView.RefreshCalendarDays();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnAppearing: {ex}");
            }
        }
    }
} 