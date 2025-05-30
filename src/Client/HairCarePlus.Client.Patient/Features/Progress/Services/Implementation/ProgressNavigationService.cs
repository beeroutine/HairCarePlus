using CommunityToolkit.Maui.Views;
using HairCarePlus.Client.Patient.Features.Progress.Views;
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

        public Task PreviewPhotoAsync(string localPath)
        {
            var popup = new PhotoPreviewPopup(localPath);
            Shell.Current.CurrentPage?.ShowPopup(popup);
            return Task.CompletedTask;
        }

        public Task ShowDescriptionAsync(string description)
        {
            var sheet = new DescriptionSheet(description);
            Shell.Current.CurrentPage?.ShowPopup(sheet);
            return Task.CompletedTask;
        }

        public Task ShowRestrictionDetailsAsync(RestrictionTimer timer)
        {
            var popup = new RestrictionDetailPopup(timer);
            Shell.Current.CurrentPage?.ShowPopup(popup);
            return Task.CompletedTask;
        }

        public Task ShowAllRestrictionsAsync(IReadOnlyList<RestrictionTimer> timers)
        {
            var popup = new AllRestrictionsPopup(timers);
            Shell.Current.CurrentPage?.ShowPopup(popup);
            return Task.CompletedTask;
        }

        public Task ShowProcedureChecklistAsync()
        {
            var vm = _sp.GetRequiredService<ViewModels.ProcedureChecklistViewModel>();
            var popup = new ProcedureChecklistPopup(vm);
            Shell.Current.CurrentPage?.ShowPopup(popup);
            return Task.CompletedTask;
        }

        public Task ShowInsightsAsync(AIReport report)
        {
            var sheet = new InsightsSheet(report);
            Shell.Current.CurrentPage?.ShowPopup(sheet);
            return Task.CompletedTask;
        }
    }
} 