using CommunityToolkit.Maui.Views;
using HairCarePlus.Client.Patient.Features.Progress.Views;
using MauiApp = Microsoft.Maui.Controls.Application;
using HairCarePlus.Client.Patient.Features.PhotoCapture.Views;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using HairCarePlus.Client.Patient.Infrastructure.Services;
using HairCarePlus.Client.Patient.Features.Progress.Services.Interfaces;
using HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;
using HairCarePlus.Shared.Common.CQRS;
using CommunityToolkit.Maui.Extensions;

namespace HairCarePlus.Client.Patient.Features.Progress.Services.Implementation
{
    public class ProgressNavigationService : IProgressNavigationService
    {
        private readonly INavigationService _nav;
        private readonly IServiceProvider _sp;

        public ProgressNavigationService(INavigationService nav, IServiceProvider sp)
        {
            _nav = nav;
            _sp = sp;
        }

        public Task NavigateToCameraAsync() => _nav.NavigateToAsync("//camera");

        public async Task PreviewPhotoAsync(string localPath)
        {
            var popup = new PhotoPreviewPopup(localPath);
            var page = MauiApp.Current?.Windows?.FirstOrDefault()?.Page ?? MauiApp.Current?.MainPage;
            if (page is not null)
            {
                await page.ShowPopupAsync(popup);
            }
        }

        public async Task ShowDescriptionAsync(string description)
        {
            var sheet = new DescriptionSheet(description);
            var page = MauiApp.Current?.Windows?.FirstOrDefault()?.Page ?? MauiApp.Current?.MainPage;
            if (page is not null)
            {
                await page.ShowPopupAsync(sheet);
            }
        }

        public async Task ShowRestrictionDetailsAsync(RestrictionTimer timer)
        {
            var popup = new RestrictionDetailPopup(timer);
            var page = MauiApp.Current?.Windows?.FirstOrDefault()?.Page ?? MauiApp.Current?.MainPage;
            if (page is not null)
            {
                await page.ShowPopupAsync(popup);
            }
        }

        public async Task ShowAllRestrictionsAsync(IReadOnlyList<RestrictionTimer> timers)
        {
            var popup = new AllRestrictionsPopup(timers);
            var page = MauiApp.Current?.Windows?.FirstOrDefault()?.Page ?? MauiApp.Current?.MainPage;
            if (page is not null)
            {
                await page.ShowPopupAsync(popup);
            }
        }

        public async Task ShowProcedureChecklistAsync()
        {
            var vm = _sp.GetRequiredService<ViewModels.ProcedureChecklistViewModel>();
            var popup = new ProcedureChecklistPopup(vm);
            var page = MauiApp.Current?.Windows?.FirstOrDefault()?.Page ?? MauiApp.Current?.MainPage;
            if (page is not null)
            {
                await page.ShowPopupAsync(popup);
            }
        }

        public async Task ShowInsightsAsync(AIReport report)
        {
            var sheet = new InsightsSheet(report);
            var page = MauiApp.Current?.Windows?.FirstOrDefault()?.Page ?? MauiApp.Current?.MainPage;
            if (page is not null)
            {
                await page.ShowPopupAsync(sheet);
            }
        }
    }
} 