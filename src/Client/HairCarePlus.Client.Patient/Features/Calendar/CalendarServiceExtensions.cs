using System;
using System.Net.Http;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;
using HairCarePlus.Client.Patient.Features.Calendar.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Controls;
using static Microsoft.Maui.Controls.Routing;

namespace HairCarePlus.Client.Patient.Features.Calendar
{
    public static class CalendarServiceExtensions
    {
        /// <summary>
        /// Registers all the calendar-related services with the DI container
        /// </summary>
        public static IServiceCollection AddCalendarServices(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            
            // Register calendar services
            services.AddSingleton<ICalendarService, CalendarService>();
            services.AddSingleton<INotificationService, NotificationService>();
            
            // Register calendar ViewModels
            services.AddTransient<CalendarViewModel>();
            services.AddTransient<HorizontalCalendarViewModel>();
            services.AddTransient<RestrictionTimersViewModel>();
            
            // Register calendar views
            services.AddTransient<CalendarPage>();
            
            return services;
        }
        
        /// <summary>
        /// Registers routes for calendar navigation
        /// </summary>
        public static MauiAppBuilder RegisterCalendarRoutes(this MauiAppBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            
            // Register routes for calendar navigation
            RegisterRoute("calendar", typeof(CalendarPage));
            
            return builder;
        }
    }
} 