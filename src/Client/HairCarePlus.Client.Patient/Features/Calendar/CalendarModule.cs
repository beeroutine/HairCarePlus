using HairCarePlus.Client.Patient.Features.Calendar.Services;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using NotificationsImpl = HairCarePlus.Client.Patient.Features.Notifications.Services;
using NotificationsService = HairCarePlus.Client.Patient.Features.Notifications.Services.Interfaces;

namespace HairCarePlus.Client.Patient.Features.Calendar
{
    public static class CalendarModule
    {
        public static IServiceCollection AddCalendarModule(this IServiceCollection services)
        {
            // Register services
            services.AddSingleton<ICalendarService, CalendarService>();
            services.AddSingleton<NotificationsService.INotificationService, NotificationsImpl.NotificationService>();
            services.AddSingleton<IEventAggregationService, EventAggregationService>();
            
            // Register view models
            services.AddTransient<CalendarViewModel>();
            services.AddTransient<CleanCalendarViewModel>();
            
            return services;
        }
    }
} 