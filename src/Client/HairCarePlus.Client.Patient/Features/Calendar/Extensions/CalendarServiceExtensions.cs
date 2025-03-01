using HairCarePlus.Client.Patient.Features.Calendar.Services;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;
using HairCarePlus.Client.Patient.Features.Calendar.Views;

namespace HairCarePlus.Client.Patient.Features.Calendar.Extensions
{
    public static class CalendarServiceExtensions
    {
        public static IServiceCollection AddCalendarFeature(this IServiceCollection services)
        {
            // Services
            services.AddSingleton<ICalendarService, PostOperationCalendarService>();

            // ViewModels
            services.AddTransient<CalendarViewModel>();
            services.AddTransient<DayDetailsViewModel>();
            services.AddTransient<ProgressViewModel>();
            services.AddTransient<MonthViewModel>();
            services.AddTransient<WeekViewModel>();
            services.AddTransient<ListViewModel>();
            services.AddTransient<DayTodoViewModel>();

            // Views
            services.AddTransient<CalendarPage>();
            services.AddTransient<DayDetailsPage>();
            services.AddTransient<ProgressPage>();
            services.AddTransient<MonthView>();
            services.AddTransient<WeekView>();
            services.AddTransient<EventListView>();

            return services;
        }

        public static MauiAppBuilder RegisterCalendarRoutes(this MauiAppBuilder builder)
        {
            Routing.RegisterRoute(nameof(CalendarPage), typeof(CalendarPage));
            Routing.RegisterRoute(nameof(DayDetailsPage), typeof(DayDetailsPage));
            Routing.RegisterRoute(nameof(ProgressPage), typeof(ProgressPage));
            Routing.RegisterRoute(nameof(MonthView), typeof(MonthView));
            Routing.RegisterRoute(nameof(WeekView), typeof(WeekView));
            Routing.RegisterRoute(nameof(EventListView), typeof(EventListView));

            return builder;
        }
    }
} 