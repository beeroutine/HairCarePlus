using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;
using HairCarePlus.Client.Patient.Infrastructure.Services.Interfaces;
using HairCarePlus.Shared.Common.CQRS;
using Microsoft.Extensions.Logging;
using Microsoft.Reactive.Testing;
using Moq;
using NUnit.Framework;
using ReactiveUI.Testing;
using CommunityToolkit.Mvvm.Messaging;

namespace HairCarePlus.Client.Patient.Features.Calendar.Tests
{
    [TestFixture]
    public class TodayViewModelReactiveTests
    {
        private TestScheduler _scheduler;
        private Mock<ICalendarService> _calendarServiceMock;
        private Mock<ICalendarCacheService> _cacheServiceMock;
        private Mock<ICalendarLoader> _eventLoaderMock;
        private Mock<IProgressCalculator> _progressCalculatorMock;
        private Mock<ILogger<TodayViewModelReactive>> _loggerMock;
        private Mock<ICommandBus> _commandBusMock;
        private Mock<IQueryBus> _queryBusMock;
        private Mock<IMessenger> _messengerMock;
        private Mock<IProfileService> _profileServiceMock;
        
        [SetUp]
        public void Setup()
        {
            _scheduler = new TestScheduler();
            _calendarServiceMock = new Mock<ICalendarService>();
            _cacheServiceMock = new Mock<ICalendarCacheService>();
            _eventLoaderMock = new Mock<ICalendarLoader>();
            _progressCalculatorMock = new Mock<IProgressCalculator>();
            _loggerMock = new Mock<ILogger<TodayViewModelReactive>>();
            _commandBusMock = new Mock<ICommandBus>();
            _queryBusMock = new Mock<IQueryBus>();
            _messengerMock = new Mock<IMessenger>();
            _profileServiceMock = new Mock<IProfileService>();
            
            // Set default surgery date
            _profileServiceMock.Setup(x => x.SurgeryDate).Returns(DateTime.Today.AddDays(-30));
        }
        
        private TodayViewModelReactive CreateViewModel()
        {
            return new TodayViewModelReactive(
                _calendarServiceMock.Object,
                _cacheServiceMock.Object,
                _eventLoaderMock.Object,
                _progressCalculatorMock.Object,
                _loggerMock.Object,
                _commandBusMock.Object,
                _queryBusMock.Object,
                _messengerMock.Object,
                _profileServiceMock.Object);
        }
        
        [Test]
        public void Constructor_InitializesWithTodayDate()
        {
            // Arrange & Act
            var vm = CreateViewModel();
            
            // Assert
            Assert.AreEqual(DateTime.Today, vm.SelectedDate);
            Assert.AreEqual(DateTime.Today, vm.VisibleDate);
            Assert.AreEqual("Today", vm.Title);
        }
        
        [Test]
        public void GoToTodayCommand_ThrottlesRapidTaps()
        {
            // Arrange
            _scheduler.With(scheduler =>
            {
                var vm = CreateViewModel();
                var executionCount = 0;
                
                vm.GoToTodayCommand.Subscribe(_ => executionCount++);
                
                // Act - simulate rapid taps
                vm.GoToTodayCommand.Execute().Subscribe();
                scheduler.AdvanceBy(TimeSpan.FromMilliseconds(100).Ticks);
                vm.GoToTodayCommand.Execute().Subscribe();
                scheduler.AdvanceBy(TimeSpan.FromMilliseconds(100).Ticks);
                vm.GoToTodayCommand.Execute().Subscribe();
                
                // Wait for throttle period
                scheduler.AdvanceBy(TimeSpan.FromMilliseconds(500).Ticks);
                
                // Assert - only one execution should occur
                Assert.AreEqual(1, executionCount);
            });
        }
        
        [Test]
        public void SelectedDate_TriggersEventLoading_WithThrottle()
        {
            // Arrange
            _scheduler.With(scheduler =>
            {
                var vm = CreateViewModel();
                vm.Activator.Activate();
                
                // Setup mock to return empty events
                _queryBusMock.Setup(x => x.SendAsync<IEnumerable<CalendarEvent>>(
                    It.IsAny<IQuery<IEnumerable<CalendarEvent>>>()))
                    .ReturnsAsync(new List<CalendarEvent>());
                
                // Act - change selected date multiple times
                vm.SelectedDate = DateTime.Today.AddDays(1);
                scheduler.AdvanceBy(TimeSpan.FromMilliseconds(100).Ticks);
                vm.SelectedDate = DateTime.Today.AddDays(2);
                scheduler.AdvanceBy(TimeSpan.FromMilliseconds(100).Ticks);
                vm.SelectedDate = DateTime.Today.AddDays(3);
                
                // Wait for throttle period
                scheduler.AdvanceBy(TimeSpan.FromMilliseconds(300).Ticks);
                
                // Assert - only last date should trigger loading
                _queryBusMock.Verify(x => x.SendAsync<IEnumerable<CalendarEvent>>(
                    It.IsAny<IQuery<IEnumerable<CalendarEvent>>>()), Times.Once);
            });
        }
        
        [Test]
        public void CompletionProgress_UpdatesWhenEventsChange()
        {
            // Arrange
            _scheduler.With(scheduler =>
            {
                var vm = CreateViewModel();
                var progressUpdates = new List<double>();
                
                vm.WhenAnyValue(x => x.CompletionProgress)
                    .Subscribe(progress => progressUpdates.Add(progress));
                
                // Setup progress calculator
                _progressCalculatorMock.Setup(x => x.CalculateProgress(It.IsAny<IEnumerable<CalendarEvent>>()))
                    .Returns((0.5, 50));
                
                // Act - activate and load events
                vm.Activator.Activate();
                scheduler.AdvanceBy(TimeSpan.FromMilliseconds(200).Ticks);
                
                // Assert
                Assert.IsTrue(progressUpdates.Count > 0);
                Assert.Contains(0.5, progressUpdates);
            });
        }
        
        [Test]
        public void FormattedSelectedDate_UpdatesWithSelectedDate()
        {
            // Arrange
            var vm = CreateViewModel();
            var testDate = new DateTime(2024, 12, 25);
            
            // Act
            vm.SelectedDate = testDate;
            
            // Assert
            Assert.AreEqual("Wed, Dec 25", vm.FormattedSelectedDate);
        }
        
        [Test]
        public void TodayDay_UpdatesEveryHour()
        {
            // Arrange
            _scheduler.With(scheduler =>
            {
                var vm = CreateViewModel();
                var dayUpdates = new List<int>();
                
                vm.WhenAnyValue(x => x.TodayDay)
                    .Subscribe(day => dayUpdates.Add(day));
                
                // Act - advance time by 2 hours
                scheduler.AdvanceBy(TimeSpan.FromHours(2).Ticks);
                
                // Assert - should have initial value plus 2 updates
                Assert.AreEqual(3, dayUpdates.Count);
            });
        }
        
        [Test]
        public void ToggleEventCompletionCommand_OnlyEnabledForToday()
        {
            // Arrange
            _scheduler.With(scheduler =>
            {
                var vm = CreateViewModel();
                var canExecuteValues = new List<bool>();
                
                vm.ToggleEventCompletionCommand.CanExecute
                    .Subscribe(canExecute => canExecuteValues.Add(canExecute));
                
                // Act
                vm.SelectedDate = DateTime.Today;
                scheduler.AdvanceBy(1);
                vm.SelectedDate = DateTime.Today.AddDays(1);
                scheduler.AdvanceBy(1);
                vm.SelectedDate = DateTime.Today;
                scheduler.AdvanceBy(1);
                
                // Assert
                Assert.AreEqual(3, canExecuteValues.Count);
                Assert.IsTrue(canExecuteValues[0]); // Today - enabled
                Assert.IsFalse(canExecuteValues[1]); // Tomorrow - disabled
                Assert.IsTrue(canExecuteValues[2]); // Today again - enabled
            });
        }
        
        [Test]
        public void Dispose_CleansUpSubscriptions()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.Activator.Activate();
            
            // Act
            vm.Activator.Deactivate();
            vm.Dispose();
            
            // Assert - no exceptions should be thrown
            Assert.DoesNotThrow(() => vm.SelectedDate = DateTime.Today.AddDays(1));
        }
    }
} 