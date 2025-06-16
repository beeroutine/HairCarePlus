# Today Page (Patient App)

> Updated: June 2025 | Tested on .NET MAUI 9.0.51 SR / iOS 17 & Android 14

---

## Purpose
The Today Page is the home-screen of the Patient application. It shows the list of calendar events for the **selected date** (today by default) and gives the patient quick feedback on recovery progress via an animated progress ring.

## Key Features (current implementation)
* Shows **today's date** and **day-counter since surgery** in the header.
* Horizontal **date selector** built with `CollectionView` + `VisualStateManager`.
* Vertical list of **calendar events** (medications, photo tasks, information notes, etc.).
* Swipe **left-to-right** on an event to mark it **completed / un-completed**.
* **Animated progress ring** around today's date using a custom `SkiaProgressRing` control (SkiaSharp).
* **Confetti** celebration when 100 % of the day's tasks are completed.
* Light / Dark theme support via `ResourceDictionary` + `AppThemeBinding`.
* Runs completely **offline-first**; data is cached locally and synchronised on demand.

> ❗ No ReactiveUI is used in runtime. The page relies on classic MVVM (`INotifyPropertyChanged` + CommunityToolkit commands).

## UI Structure
| Zone | MAUI controls | Notes |
|------|---------------|-------|
| Header | `Border` + `SkiaProgressRing` + `VerticalStackLayout` | Tap on the ring executes `GoToTodayCommand`.
| Date selector | Horizontal `CollectionView` with `LinearItemsLayout` | `VisualStateManager` switches between *Normal* and *Selected* states.
| Event list | Vertical `CollectionView` | Each cell is a `SwipeView` containing a styled `Border` card.
| Confetti overlay | `SKConfettiView` (SkiaSharp.Extended.UI) | Covers whole page; disabled until progress reaches 100 %.

## View / ViewModel mapping
```
Views/TodayPage.xaml          --> View (compiled bindings, x:DataType)
Views/TodayPage.xaml.cs       --> View-code (platform tweaks, event handlers)
ViewModels/TodayViewModel.cs  --> ViewModel (CommunityToolkit MVVM)
```

## Important Bindings
| View property | ViewModel property |
|---------------|-------------------|
| `ProgressRingRef.Progress` | `CompletionProgress` (double 0-1) |
| `DateSelectorView.ItemsSource` | `CalendarDays` (ObservableCollection<DateTime>) |
| `DateSelectorView.SelectedItem` | `SelectedDate` (Two-way) |
| Event card `SwipeItem.Command` | `ToggleEventCompletionCommand` |

## Interaction Flow
1. `TodayPage.OnAppearing` → `TodayViewModel.EnsureLoadedAsync()`
2. ViewModel populates `CalendarDays`, loads events for `SelectedDate`, calculates `CompletionProgress`.
3. User swipes an event card → `ToggleEventCompletionCommand` (CQRS **Command**) updates DB   → publishes `EventUpdatedMessage`.
4. ViewModel recalculates progress; property-change animates the ring via `SkiaProgressRing`.
5. When `CompletionProgress` reaches `1.0`, `SkiaProgressRing.Completed` fires → View displays confetti.

## Technical Highlights
* **Clean Architecture**: UI → Application (CQRS) → Domain → Infrastructure.
* **MVVM**: CommunityToolkit.Mvvm (`ObservableObject`, `RelayCommand`).
* **CQRS**: `ToggleEventCompletionCommand`, `GetEventsForDateQuery` handled via in-memory buses.
* **Caching**: `ICalendarCacheService` keeps recent queries in memory to minimise DB hits.
* **SkiaSharp**: Custom control animates progress; uses `Loaded`/`Unloaded` to pause when page is hidden.
* **Accessibility**: High-contrast colours and semantic icons (`MaterialIcons` font).

## DI Registration
The Calendar module is wired in `CalendarServiceExtensions.cs`:
```csharp
services.AddCalendarServices(); // extension method

// Inside AddCalendarServices():
services.AddTransient<TodayViewModel>();
services.AddTransient<TodayPage>();
// … plus command/query handlers and services
```
Shell route:
```csharp
Routing.RegisterRoute("today", typeof(TodayPage));
```

## Removed / Not Implemented
* **Long-press** gesture (used in early prototype) — replaced by swipe.
* ReactiveUI (`TodayPageReactive`, `TodayViewModelReactive`) — deleted from codebase.
* Persistence of last-selected date — currently always defaults to *today* on launch.

---
© HairCare+ 2025 