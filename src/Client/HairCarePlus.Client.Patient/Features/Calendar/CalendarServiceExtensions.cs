using System;
using System.Collections.Generic;
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
using HairCarePlus.Client.Patient.Infrastructure.Features.Calendar.Repositories;
using HairCarePlus.Client.Patient.Infrastructure.Storage;
using HairCarePlus.Client.Patient.Features.Calendar.Application.Commands;
using HairCarePlus.Client.Patient.Features.Calendar.Application.Queries;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Shared.Common.CQRS;
// Alias to avoid conflict with Application namespace
using MauiApp = Microsoft.Maui.Controls.Application;

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
            services.AddScoped<IRestrictionService, RestrictionService>();
            services.AddSingleton<ICalendarCacheService, CalendarCacheService>();
            services.AddSingleton<ICalendarLoader, CalendarLoaderService>();
            services.AddSingleton<IProgressCalculator, ProgressCalculatorService>();
            
            // Register repository
            services.AddScoped<IHairTransplantEventRepository, CalendarRepository>();
            
            // Register the Notifications.Services notification service
            services.AddSingleton<Notifications.Services.Interfaces.INotificationService, Notifications.Services.NotificationService>();
            
            // Register EventAggregationService
            services.AddSingleton<IEventAggregationService, EventAggregationServiceImpl>();
            
            // Register ViewModels
            services.AddTransient<TodayViewModel>();
            services.AddTransient<EventDetailViewModel>();
            
            // CQRS kernel & handlers
            services.AddCqrs();
            services.AddScoped<ToggleEventCompletionHandler>();
            services.AddScoped<ICommandHandler<ToggleEventCompletionCommand>, ToggleEventCompletionHandler>();
            
            // Query handlers
            services.AddScoped<IQueryHandler<GetEventsForDateQuery, IEnumerable<CalendarEvent>>, GetEventsForDateHandler>();
            services.AddScoped<IQueryHandler<GetEventCountsForDatesQuery, Dictionary<DateTime, Dictionary<EventType, int>>>, GetEventCountsForDatesHandler>();
            services.AddScoped<IQueryHandler<GetActiveRestrictionsQuery, IReadOnlyList<RestrictionInfo>>, GetActiveRestrictionsHandler>();
            
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
            if (MauiApp.Current?.Resources == null)
                return;
                
            // Add converters to application resources if they don't exist
            if (!MauiApp.Current.Resources.ContainsKey("DateToColorConverter"))
                MauiApp.Current.Resources.Add("DateToColorConverter", new DateToColorConverter());
                
            if (!MauiApp.Current.Resources.ContainsKey("EventTypeToColorConverter"))
                MauiApp.Current.Resources.Add("EventTypeToColorConverter", new EventTypeToColorConverter());
                
            if (!MauiApp.Current.Resources.ContainsKey("EventTypeToIconConverter"))
                MauiApp.Current.Resources.Add("EventTypeToIconConverter", new EventTypeToIconConverter());
                
            if (!MauiApp.Current.Resources.ContainsKey("EventPriorityToColorConverter"))
                MauiApp.Current.Resources.Add("EventPriorityToColorConverter", new EventPriorityToColorConverter());
                
            if (!MauiApp.Current.Resources.ContainsKey("EventPriorityToIconConverter"))
                MauiApp.Current.Resources.Add("EventPriorityToIconConverter", new EventPriorityToIconConverter());
                
            if (!MauiApp.Current.Resources.ContainsKey("DateHasEventTypeConverter"))
                MauiApp.Current.Resources.Add("DateHasEventTypeConverter", new DateHasEventTypeConverter());
                
            if (!MauiApp.Current.Resources.ContainsKey("EventIndicatorsConverter"))
                MauiApp.Current.Resources.Add("EventIndicatorsConverter", new EventIndicatorsConverter());

            if (!MauiApp.Current.Resources.ContainsKey("DaysToStrokeColorConverter"))
                MauiApp.Current.Resources.Add("DaysToStrokeColorConverter", new DaysToStrokeColorConverter());
        }
    }
} 