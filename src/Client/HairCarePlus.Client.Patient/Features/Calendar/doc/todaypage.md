# Today Page Module

> Version: September 2025 | .NET MAUI 9.0.51 SR

## TL;DR
Displays the patient's daily calendar events with offline-first support, gesture controls, and adaptive theming.

## Table of Contents
1. [Purpose](#purpose)
2. [Features](#features)
3. [UI Structure](#ui-structure)
4. [Technical Implementation](#technical-implementation)
5. [Interaction Flow](#interaction-flow)
6. [Accessibility & Performance](#accessibility--performance)
7. [Platform Considerations](#platform-considerations)
8. [Architecture](#architecture)
9. [DI Registration](#di-registration)

## Purpose
The Today Page serves as the main calendar interface in the Patient App, displaying events for the current day and enabling horizontal navigation between dates.

## Features
- Current date and days-since-transplant display
- Horizontal date selector with VisualStateManager styling
- List of events with type indicators (medication, photo, video)
- Color coding via DataTrigger and resource dictionaries
- Toggle completion via swipe-left or long-press (≥2s)
- Animated progress ring around today's date, tap-to-jump
- Adaptive Light/Dark theming
- Persistence of selected date across sessions

## UI Structure
### Header
- Large date label (`FormattedTodayDate`)
- Transplant-day counter (`DaysSinceTransplant`)

### Horizontal Calendar
- `CollectionView` (horizontal `LinearItemsLayout`)
- Items show day-of-week and day-of-month in a circular `Border`
- Selection managed by VisualStateManager

### Event List
- Vertical `CollectionView` of event cards
- Cards use `Border` with icon, time, title, description
- Completed events styled with shaded background and strikethrough
- `EmptyView` if no events

## Technical Implementation
- MVVM Pattern: `TodayPage.xaml` (View) + `TodayViewModel` (ViewModel) with compiled bindings (`x:DataType`)
- Data Model: `SelectedDate`, `CalendarDays`, `FlattenedEvents`, `EventCountsByDate`, `DaysSinceTransplant`
- Commands & Queries (via CQRS): `SelectDateCommand`, `ToggleEventCompletionCommand`, `GetEventsForDateQuery`, `GetEventCountsForDatesQuery`, etc.

## Interaction Flow
1. On appearing, `TodayViewModel` loads days and events via queries.
2. Date selection dispatches queries; UI updates accordingly.
3. Swipe/long-press dispatch `ToggleEventCompletionCommand`; handlers update the store and publish `EventUpdatedMessage`.
4. ViewModel listens for messages and refreshes the UI.
5. All logic is testable in isolation.

## Accessibility & Performance
- High-contrast colors for readability
- Virtualized `CollectionView` for efficient rendering
- Main-thread UI updates via `Dispatcher`
- Suppress flicker with `SelectionHighlightColor` and `SelectionChangedAnimationEnabled` when possible

## Platform Considerations
- `SafeAreaInsets` support on iOS to avoid UI overlap
- Platform-specific flicker suppression on Android/iOS

## Architecture
Follows Clean Architecture & CQRS within the Calendar feature:
```text
Calendar/
├── Application/
│   ├── Commands/ToggleEventCompletionCommand.cs
│   ├── Queries/GetEventsForDateQuery.cs
│   └── Messages/EventUpdatedMessage.cs
├── ViewModels/TodayViewModel.cs
├── Views/TodayPage.xaml (+ .cs)
└── doc/todaypage.md
```

## DI Registration
Register the TodayPage ViewModel and handlers in `MauiProgram.cs`:
```csharp
services.AddCqrs(); // registers command and query buses
services.AddScoped<TodayViewModel>();
services.AddScoped<ICommandHandler<ToggleEventCompletionCommand>, ToggleEventCompletionHandler>();
services.AddScoped<IQueryHandler<GetEventsForDateQuery, IEnumerable<CalendarEvent>>, GetEventsForDateHandler>();
services.AddScoped<IQueryHandler<GetEventCountsForDatesQuery, Dictionary<DateTime, Dictionary<EventType,int>>>, GetEventCountsForDatesHandler>();
``` 