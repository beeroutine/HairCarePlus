# Using ReactiveUI Calendar Module

## Quick Start

To use the new ReactiveUI version of the Calendar module:

### 1. Update Navigation

Replace the old navigation:
```csharp
// Old
await Shell.Current.GoToAsync("today");

// New
await Shell.Current.GoToAsync("today-reactive");
```

### 2. Update DI Registration (if using custom setup)

```csharp
// Register reactive versions
services.AddTransient<TodayViewModelReactive>();
services.AddTransient<TodayPageReactive>();
```

### 3. Key Differences

#### Old Way (Traditional MVVM)
- Multiple behaviors causing race conditions
- Manual throttling with timestamps
- Complex state synchronization
- Platform-specific issues on Android

#### New Way (ReactiveUI)
- Single unified behavior
- Built-in throttling and debouncing
- Automatic state synchronization
- Platform-optimized for both iOS and Android

### 4. Performance Improvements

**Android:**
- ✅ Smooth scrolling with native RecyclerView optimization
- ✅ Proper centering animation
- ✅ No gray selection overlays
- ✅ Throttled GoToToday command

**iOS:**
- ✅ Maintained excellent performance
- ✅ Cleaner code with reactive patterns
- ✅ Better memory management

### 5. Testing

The reactive version provides better testability:

```csharp
// Example: Testing command throttling
[Test]
public void GoToToday_ThrottlesRapidTaps()
{
    _scheduler.With(scheduler =>
    {
        var vm = new TodayViewModelReactive(...);
        
        // Simulate rapid taps
        vm.GoToTodayCommand.Execute();
        scheduler.AdvanceBy(100);
        vm.GoToTodayCommand.Execute();
        
        // Only one execution occurs
    });
}
```

### 6. Debugging

Enable ReactiveUI debugging in development:

```csharp
#if DEBUG
RxApp.DefaultExceptionHandler = Observer.Create<Exception>(ex => 
{
    Debug.WriteLine($"ReactiveUI: {ex}");
});
#endif
```

### 7. Common Scenarios

#### Selecting Today's Date
```csharp
// Automatically throttled - no need to worry about rapid taps
vm.GoToTodayCommand.Execute();
```

#### Selecting a Specific Date
```csharp
// Automatically centers and loads events
vm.SelectDateCommand.Execute(targetDate);
```

#### Toggling Event Completion
```csharp
// Only enabled for today's events
vm.ToggleEventCompletionCommand.Execute(calendarEvent);
```

### 8. Migration Path

1. Both versions can coexist during migration
2. Start with high-traffic pages (Today Page)
3. Gradually migrate other ViewModels
4. Remove old versions once stable

## Troubleshooting

### Issue: Build errors after adding ReactiveUI
**Solution:** Ensure all packages are restored and FodyWeavers.xml exists

### Issue: Bindings not updating
**Solution:** Check [Reactive] attributes on properties

### Issue: Android performance issues
**Solution:** Verify ReactiveCalendarBehavior is properly attached

### Issue: Memory leaks
**Solution:** Ensure proper disposal in WhenActivated

## Next Steps

- Monitor performance metrics
- Gather user feedback
- Plan migration for other modules
- Consider adding reactive validation

For detailed technical information, see [ReactiveUI Migration Guide](./reactiveui-migration.md) 