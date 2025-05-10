using System;
using HairCarePlus.Shared.Common.CQRS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Controls;
using static Microsoft.Maui.Controls.Routing;
using HairCarePlus.Client.Patient.Features.PhotoCapture.Services.Interfaces;
using HairCarePlus.Client.Patient.Features.PhotoCapture.Services.Implementation;
using HairCarePlus.Client.Patient.Features.PhotoCapture.ViewModels;
using HairCarePlus.Client.Patient.Features.PhotoCapture.Application.Commands;
using HairCarePlus.Client.Patient.Features.PhotoCapture.Views;
using HairCarePlus.Client.Patient.Features.PhotoCapture.Application.Queries;
using HairCarePlus.Client.Patient.Features.PhotoCapture.Domain.Entities;

namespace HairCarePlus.Client.Patient.Features.PhotoCapture;

public static class PhotoCaptureServiceExtensions
{
    public static IServiceCollection AddPhotoCaptureFeature(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        services.AddScoped<ICameraArService, CameraArService>();
        services.AddTransient<PhotoCaptureViewModel>();
        services.AddTransient<PhotoCapturePage>();

        // CQRS
        services.AddCqrs();
        services.AddScoped<ICommandHandler<CapturePhotoCommand>, CapturePhotoHandler>();
        services.AddScoped<IQueryHandler<GetCaptureTemplatesQuery, IReadOnlyList<CaptureTemplate>>, GetCaptureTemplatesHandler>();

        return services;
    }

    public static MauiAppBuilder RegisterPhotoCaptureRoutes(this MauiAppBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        RegisterRoute("camera", typeof(PhotoCapturePage));
        return builder;
    }
} 