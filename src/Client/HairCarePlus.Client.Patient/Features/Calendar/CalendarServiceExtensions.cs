using System;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Implementation;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;
using HairCarePlus.Client.Patient.Features.Calendar.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Controls;
using static Microsoft.Maui.Controls.Routing;
using HairCarePlus.Client.Patient.Features.Notifications.Services;
using HairCarePlus.Client.Patient.Features.Notifications.Services.Interfaces;
using HairCarePlus.Client.Patient.Features.Calendar.Converters;
using HairCarePlus.Client.Patient.Features.Calendar.Domain.Repositories;
using HairCarePlus.Client.Patient.Features.Calendar.Infrastructure.Repositories;
using HairCarePlus.Client.Patient.Infrastructure.Storage;

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
            
            // Register services
            services.AddScoped<ICalendarService, HairTransplantEventService>();
            services.AddScoped<IHairTransplantEventService, HairTransplantEventService>();
            services.AddScoped<IRestrictionService, RestrictionService>();
            services.AddSingleton<ICalendarCacheService, CalendarCacheService>();
            services.AddSingleton<ICalendarLoader, CalendarLoaderService>();
            services.AddSingleton<IProgressCalculator, ProgressCalculatorService>();
            
            // Register repository
            services.AddScoped<ICalendarRepository, CalendarRepository>();
            
            // Register the Notifications.Services notification service
            services.AddSingleton<Notifications.Services.Interfaces.INotificationService, Notifications.Services.NotificationService>();
            
            // Register EventAggregationService
            services.AddSingleton<IEventAggregationService, EventAggregationServiceImpl>();
            
            // Register ViewModels
            services.AddTransient<TodayViewModel>();
            services.AddTransient<EventDetailViewModel>();
            
            // Register calendar views
            services.AddTransient<TodayPage>();
            services.AddTransient<EventDetailPage>();
            
            // Register converters as resources
            RegisterConverters();
            
            // Data initializer & generators
            services.AddSingleton<IHairTransplantEventGenerator, JsonHairTransplantEventGenerator>();
            services.AddSingleton<IDataInitializer, CalendarDataInitializer>();
            
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
            RegisterRoute("today", typeof(TodayPage));
            RegisterRoute("calendar/event", typeof(EventDetailPage));
            
            return builder;
        }
        
        /// <summary>
        /// Registers converters as application resources
        /// </summary>
        private static void RegisterConverters()
        {
            if (Application.Current?.Resources == null)
                return;
                
            // Add converters to application resources if they don't exist
            if (!Application.Current.Resources.ContainsKey("DateToColorConverter"))
                Application.Current.Resources.Add("DateToColorConverter", new DateToColorConverter());
                
            if (!Application.Current.Resources.ContainsKey("EventTypeToColorConverter"))
                Application.Current.Resources.Add("EventTypeToColorConverter", new EventTypeToColorConverter());
                
            if (!Application.Current.Resources.ContainsKey("EventTypeToIconConverter"))
                Application.Current.Resources.Add("EventTypeToIconConverter", new EventTypeToIconConverter());
                
            if (!Application.Current.Resources.ContainsKey("EventPriorityToColorConverter"))
                Application.Current.Resources.Add("EventPriorityToColorConverter", new EventPriorityToColorConverter());
                
            if (!Application.Current.Resources.ContainsKey("EventPriorityToIconConverter"))
                Application.Current.Resources.Add("EventPriorityToIconConverter", new EventPriorityToIconConverter());
                
            if (!Application.Current.Resources.ContainsKey("DateHasEventTypeConverter"))
                Application.Current.Resources.Add("DateHasEventTypeConverter", new DateHasEventTypeConverter());
                
            if (!Application.Current.Resources.ContainsKey("EventIndicatorsConverter"))
                Application.Current.Resources.Add("EventIndicatorsConverter", new EventIndicatorsConverter());
        }
    }
} 