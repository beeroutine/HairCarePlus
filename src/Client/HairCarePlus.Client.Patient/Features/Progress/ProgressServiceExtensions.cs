using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using static Microsoft.Maui.Controls.Routing;
using HairCarePlus.Client.Patient.Features.Progress.ViewModels;
using HairCarePlus.Client.Patient.Features.Progress.Views;

namespace HairCarePlus.Client.Patient.Features.Progress;

public static class ProgressServiceExtensions
{
    /// <summary>
    /// Registers Progress feature services, ViewModels and pages with the DI container.
    /// </summary>
    public static IServiceCollection AddProgressFeature(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        // Presentation-layer registrations
        services.AddTransient<ProgressViewModel>();
        services.AddTransient<ProgressPage>();

        // NOTE: When domain/application services are implemented (e.g., IRestrictionService),
        // register them here following SOLID & Clean Architecture guidelines.

        return services;
    }

    /// <summary>
    /// Registers navigation routes for the Progress feature.
    /// </summary>
    public static MauiAppBuilder RegisterProgressRoutes(this MauiAppBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        RegisterRoute("progress", typeof(ProgressPage));
        return builder;
    }
} 