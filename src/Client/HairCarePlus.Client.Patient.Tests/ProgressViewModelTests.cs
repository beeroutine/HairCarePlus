using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;
using HairCarePlus.Client.Patient.Features.Progress.ViewModels;
using HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;
using HairCarePlus.Shared.Common.CQRS;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Collections.ObjectModel;
using HairCarePlus.Client.Patient.Infrastructure.Services.Interfaces;
using System;

namespace HairCarePlus.Client.Patient.Features.Progress.Tests
{
    public class ProgressViewModelTests
    {
        [Fact]
        public async Task LoadAsync_PopulatesFeedAndRestrictions() 
        {
            // Arrange
            var mockQueryBus = new Mock<IQueryBus>();
            var mockLogger = new Mock<ILogger<ProgressViewModel>>();
            var mockProfile = new Mock<IProfileService>();
            mockProfile.Setup(p => p.SurgeryDate).Returns(DateTime.Today.AddDays(-10));
            // Prepare feed items and restrictions
            var feedItem = new ProgressFeedItem(
                Date: DateOnly.FromDateTime(DateTime.Today),
                Title: "Day 11",
                Description: "Desc",
                Photos: new List<ProgressPhoto>(),
                ActiveRestrictions: new List<RestrictionTimer>(),
                AiReport: null)
            { DoctorReportSummary = "Doc" };
            mockQueryBus.Setup(q => q.SendAsync(It.IsAny<Application.Queries.GetProgressFeedQuery>()))
                .ReturnsAsync(new[] { feedItem });
            var restriction = new RestrictionTimer("Test", 1, 1, 1, "Desc");
            mockQueryBus.Setup(q => q.SendAsync(It.IsAny<Application.Queries.GetRestrictionsQuery>()))
                .ReturnsAsync(new[] { restriction });
            var svcProvider = new Mock<IServiceProvider>();

            var vm = new ProgressViewModel(mockQueryBus.Object, mockLogger.Object, svcProvider.Object, mockProfile.Object);
            // Act
            await Task.Delay(100); // wait for LoadAsync invoked in ctor

            // Assert
            Assert.Single(vm.Feed);
            Assert.Equal("Day 11", vm.Feed.First().Title);
            Assert.Single(vm.RestrictionTimers);
            Assert.Single(vm.VisibleRestrictionItems);
        }

        [Fact]
        public void Receive_PhotoCapturedMessage_AddsNewPhotoItem() 
        {
            // Arrange
            var mockQueryBus = new Mock<IQueryBus>();
            var mockLogger = new Mock<ILogger<ProgressViewModel>>();
            var mockProfile = new Mock<IProfileService>();
            mockProfile.Setup(p => p.SurgeryDate).Returns(DateTime.Today);
            var svcProvider = new Mock<IServiceProvider>();
            var vm = new ProgressViewModel(mockQueryBus.Object, mockLogger.Object, svcProvider.Object, mockProfile.Object);
            // Act
            var path = "path/to/photo.jpg";
            vm.Receive(new PhotoCapturedMessage(path));

            // Assert
            var todayItem = vm.Feed.FirstOrDefault(f => f.Date == DateOnly.FromDateTime(DateTime.Today));
            Assert.NotNull(todayItem);
            Assert.Single(todayItem.Photos);
            Assert.Contains(todayItem.Photos, p => p.LocalPath == path);
        }

        [Fact]
        public async Task Receive_RestrictionsChangedMessage_UpdatesRestrictions() 
        {
            // Arrange
            var mockQueryBus = new Mock<IQueryBus>();
            var mockLogger = new Mock<ILogger<ProgressViewModel>>();
            var mockProfile = new Mock<IProfileService>();
            mockProfile.Setup(p => p.SurgeryDate).Returns(DateTime.Today);
            var restriction1 = new RestrictionTimer("A", 1, 2, 2, "Desc");
            var restriction2 = new RestrictionTimer("B", 2, 3, 1, "Desc");
            mockQueryBus.SetupSequence(q => q.SendAsync(It.IsAny<Application.Queries.GetRestrictionsQuery>()))
                .ReturnsAsync(new[] { restriction1 })
                .ReturnsAsync(new[] { restriction2 });
            var svcProvider = new Mock<IServiceProvider>();
            var vm = new ProgressViewModel(mockQueryBus.Object, mockLogger.Object, svcProvider.Object, mockProfile.Object);
            await Task.Delay(100);
            Assert.Single(vm.RestrictionTimers);

            // Act
            vm.Receive(new RestrictionsChangedMessage());
            await Task.Delay(100);

            // Assert
            Assert.Single(vm.RestrictionTimers);
            Assert.Equal("B", vm.RestrictionTimers.First().Title);
        }
    }
} 