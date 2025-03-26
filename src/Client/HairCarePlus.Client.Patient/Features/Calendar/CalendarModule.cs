using HairCarePlus.Client.Patient.Features.Calendar.Helpers;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;
using HairCarePlus.Client.Patient.Features.Calendar.Views;
using Microsoft.Extensions.DependencyInjection;
using NotificationsImpl = HairCarePlus.Client.Patient.Features.Notifications.Services;
using NotificationsService = HairCarePlus.Client.Patient.Features.Notifications.Services.Interfaces;

namespace HairCarePlus.Client.Patient.Features.Calendar
{
    public static class CalendarModule
    {
        public static IServiceCollection AddCalendarModule(this IServiceCollection services)
        {
            // Регистрация конвертеров
            services.AddSingleton<BoolConverter>();
            services.AddSingleton<EventTypeToColorConverter>();
            services.AddSingleton<CountToHeightConverter>();
            
            // Регистрация сервисов
            services.AddSingleton<ICalendarService, CalendarService>();
            services.AddSingleton<NotificationsService.INotificationService, NotificationsImpl.NotificationService>();
            services.AddSingleton<IEventAggregationService, EventAggregationService>();
            
            // Регистрация ViewModels
            services.AddTransient<CalendarViewModel>();
            services.AddTransient<CleanCalendarViewModel>();
            services.AddTransient<FullCalendarViewModel>();
            
            // Регистрация представлений
            services.AddTransient<CalendarPage>();
            services.AddTransient<FullCalendarPage>();
            
            return services;
        }
    }
} 