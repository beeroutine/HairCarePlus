# ReactiveUI Migration Guide for Calendar Module

> Version: 1.0 | December 2024 | .NET MAUI 9.0.51 SR + ReactiveUI 19.5.41

## Overview

This document describes the migration of the Calendar module from traditional MVVM to ReactiveUI pattern, addressing performance and reliability issues, especially on Android.

## Motivation

### Problems Addressed
1. **Race conditions** between multiple behaviors (CenterOnSelectedBehavior, CollectionViewHeaderSyncBehavior, CollectionViewSelectionStateBehavior)
2. **Android-specific issues** with CollectionView scrolling and selection
3. **Complex state management** with multiple event sources and timers
4. **Difficult testing** of asynchronous operations

### Benefits of ReactiveUI
- **Unified data flow** - single source of truth for state changes
- **Built-in operators** - throttle, debounce, distinctUntilChanged
- **Platform-independent logic** - same code works on all platforms
- **Better testability** - time-based operations can be controlled with TestScheduler

## Architecture Changes

### Old Architecture
```
TodayViewModel : BaseViewModel (INotifyPropertyChanged)
  ├── Multiple event handlers
  ├── Manual throttling with timestamps
  └── Three separate behaviors for CollectionView
```

### New Architecture
```
TodayViewModelReactive : ReactiveBaseViewModel (ReactiveObject)
  ├── Reactive properties with [Reactive] attribute
  ├── ObservableAsPropertyHelper for computed values
  ├── ReactiveCommands with built-in CanExecute
  └── Single ReactiveCalendarBehavior with unified logic
```

## Key Components

### 1. ReactiveBaseViewModel
Base class for all reactive ViewModels:
```csharp
public abstract class ReactiveBaseViewModel : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; }
    protected virtual void HandleActivation(CompositeDisposable disposables);
}
```

### 2. TodayViewModelReactive
Reactive implementation with:
- **[Reactive] properties** for two-way binding
- **ObservableAsPropertyHelper** for computed properties
- **DynamicData SourceCache** for reactive collections
- **ReactiveCommand** with automatic throttling

### 3. ReactiveCalendarBehavior
Unified behavior that replaces three separate behaviors:
```csharp
public class ReactiveCalendarBehavior : Behavior<CollectionView>
{
    // Handles selection, scrolling, and visual states
    // Platform-specific optimizations for Android RecyclerView
}
```

### 4. TodayPageReactive
Reactive page with:
- **WhenActivated** pattern for lifecycle management
- **Reactive event handling** with proper disposal
- **Platform-specific handlers** using Observable patterns

## Migration Steps

### Step 1: Add Dependencies
```xml
<PackageReference Include="ReactiveUI.Maui" Version="19.5.41" />
<PackageReference Include="ReactiveUI" Version="19.5.41" />
<PackageReference Include="ReactiveUI.Fody" Version="19.5.41" />
<PackageReference Include="System.Reactive" Version="6.0.0" />
<PackageReference Include="DynamicData" Version="8.3.27" />
<PackageReference Include="Fody" Version="6.8.0" />
```

### Step 2: Create FodyWeavers.xml
```xml
<?xml version="1.0" encoding="utf-8"?>
<Weavers xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <ReactiveUI />
</Weavers>
```

### Step 3: Update DI Registration
```csharp
// In CalendarServiceExtensions.cs
services.AddTransient<TodayViewModelReactive>();
services.AddTransient<TodayPageReactive>();
RegisterRoute("today-reactive", typeof(TodayPageReactive));
```

### Step 4: Update Navigation
To use the reactive version:
```csharp
await Shell.Current.GoToAsync("today-reactive");
```

## Key Patterns

### Reactive Properties
```csharp
[Reactive] public DateTime SelectedDate { get; set; }
```

### Computed Properties
```csharp
_formattedSelectedDate = this
    .WhenAnyValue(x => x.SelectedDate)
    .Select(date => date.ToString("ddd, MMM d"))
    .ToProperty(this, x => x.FormattedSelectedDate);
```

### Throttled Commands
```csharp
GoToTodayCommand = ReactiveCommand.CreateFromTask(ExecuteGoToToday);
GoToTodayCommand
    .Throttle(TimeSpan.FromMilliseconds(500))
    .InvokeCommand(GoToTodayCommand);
```

### Reactive Collections
```csharp
_eventsCache
    .Connect()
    .Filter(e => !e.IsCompleted)
    .Sort(SortExpressionComparer<CalendarEvent>.Ascending(e => e.Date))
    .ObserveOn(RxApp.MainThreadScheduler)
    .Bind(out _flattenedEvents)
    .Subscribe();
```

## Performance Improvements

### Android-Specific Optimizations
- Native RecyclerView smooth scrolling
- Proper visible cells enumeration
- Optimized visual state updates

### General Improvements
- Automatic disposal of subscriptions
- Built-in throttling/debouncing
- Reduced UI thread blocking

## Testing

### Unit Testing with TestScheduler
```csharp
[Test]
public void GoToToday_Throttles_RapidTaps()
{
    var scheduler = new TestScheduler();
    var vm = new TodayViewModelReactive(..., scheduler);
    
    // Simulate rapid taps
    scheduler.AdvanceBy(100);
    vm.GoToTodayCommand.Execute().Subscribe();
    scheduler.AdvanceBy(100);
    vm.GoToTodayCommand.Execute().Subscribe();
    
    // Only one execution should occur
    Assert.AreEqual(1, executionCount);
}
```

## Migration Checklist

- [ ] Add ReactiveUI packages
- [ ] Create FodyWeavers.xml
- [ ] Create ReactiveBaseViewModel
- [ ] Migrate ViewModels to ReactiveUI
- [ ] Replace multiple behaviors with ReactiveCalendarBehavior
- [ ] Update Views to use ReactiveContentPage
- [ ] Update DI registration
- [ ] Test on Android and iOS
- [ ] Update unit tests
- [ ] Update documentation

## Troubleshooting

### Common Issues

1. **Fody errors** - Ensure FodyWeavers.xml is in the project root
2. **Binding errors** - Check [Reactive] attributes on properties
3. **Memory leaks** - Ensure proper disposal in WhenActivated
4. **Android crashes** - Verify platform-specific code is properly guarded

### Debug Tips

Enable ReactiveUI debugging:
```csharp
#if DEBUG
RxApp.DefaultExceptionHandler = Observer.Create<Exception>(ex => 
{
    Logger.LogError(ex, "ReactiveUI Exception");
});
#endif
```

## Future Enhancements

1. **Migrate all ViewModels** to ReactiveUI for consistency
2. **Add reactive validation** using ReactiveUI.Validation
3. **Implement reactive navigation** patterns
4. **Add reactive caching** with Akavache

## References

- [ReactiveUI Documentation](https://www.reactiveui.net/)
- [DynamicData Documentation](https://github.com/reactivemarbles/DynamicData)
- [System.Reactive Documentation](http://introtorx.com/)
- [MAUI ReactiveUI Samples](https://github.com/reactiveui/ReactiveUI.Samples) 