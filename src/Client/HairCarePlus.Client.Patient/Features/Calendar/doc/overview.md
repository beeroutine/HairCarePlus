# Calendar Module Overview

> Version: September 2025 | .NET MAUI 9.0.51 SR

## TL;DR
Handles scheduling and tracking of a patient's post-operative care plan with offline-first local storage and CQRS-based synchronization.

## Table of Contents
1. [Purpose](#purpose)
2. [Architecture](#architecture)
3. [Data Flow (TodayPage)](#data-flow-todaypage)
4. [DI Registration](#di-registration)
5. [UI/UX Guidelines](#uiux-guidelines)
6. [Technical Implementation](#technical-implementation)
7. [Accessibility & Performance](#accessibility--performance)
8. [Platform Considerations](#platform-considerations)

## Purpose
The Calendar module manages the planning and monitoring of a patient's post-operative schedule. It provides:
- Generation of a baseline one-year procedure schedule
- Display of events in Today, Month, and Detail views
- Tracking of completion, rescheduling, and cancellation of events
- Offline-first local storage with automatic synchronization

## Architecture (Clean Architecture + CQRS)
```text
Calendar/
├── Application/
│   ├── Commands/   # ToggleEventCompletionCommand, PostponeEventCommand, etc.
│   ├── Queries/    # GetEventsForDateQuery, GetEventCountsForDatesQuery, etc.
│   └── Messages/   # EventUpdatedMessage, etc.
├── Domain/
│   └── Entities/   # CalendarEvent, RestrictionInfo, etc.
├── Infrastructure/
│   └── Repositories/  HairTransplantEventRepository (EF Core / cache)
├── Services/         # IDataInitializer, IHairTransplantEventGenerator, etc.
├── ViewModels/       # TodayViewModel, MonthViewModel, EventDetailViewModel
└── Views/            # TodayPage, MonthPage, EventDetailPage
```
Dependencies: UI → **Application (CQRS)** → Domain → Infrastructure

## Data Flow (TodayPage)
1. TodayPage triggers `LoadCalendarDaysAsync()` in `TodayViewModel` on appearing
2. ViewModel dispatches `GetEventsForDateQuery` and `GetEventCountsForDatesQuery` via `IQueryBus`
3. User gestures (swipe/long-press) send `ToggleEventCompletionCommand` via `ICommandBus`
4. Command handlers update the repository and publish `EventUpdatedMessage`; ViewModel listens and refreshes the UI
5. On first launch, `CalendarDataInitializer` and `JsonHairTransplantEventGenerator` seed initial events

## DI Registration
```csharp
services.AddCqrs() // registers InMemoryCommandBus / QueryBus
        // Infrastructure
        .AddScoped<IHairTransplantEventRepository, HairTransplantEventRepository>()
        .AddSingleton<IHairTransplantEventGenerator, JsonHairTransplantEventGenerator>()
        .AddScoped<IDataInitializer, CalendarDataInitializer>()
        // Query handlers
        .AddScoped<IQueryHandler<GetEventsForDateQuery, IEnumerable<CalendarEvent>>, GetEventsForDateHandler>()
        .AddScoped<IQueryHandler<GetEventCountsForDatesQuery, Dictionary<DateTime, Dictionary<EventType,int>>>, GetEventCountsForDatesHandler>()
        .AddScoped<IQueryHandler<GetActiveRestrictionsQuery, IReadOnlyList<RestrictionInfo>>, GetActiveRestrictionsHandler>()
        // Command handlers
        .AddScoped<ICommandHandler<ToggleEventCompletionCommand>, ToggleEventCompletionHandler>();
```

## UI/UX Guidelines
- Use **Shell**, **CollectionView**, **Border**, and **VisualStateManager** for UI composition
- Enable compiled bindings (`x:DataType`) in XAML
- Theme via **ResourceDictionary** and **AppThemeBinding**; avoid hard-coded colors
- Leverage **SwipeView** and **TouchBehavior** for gestures

## Technical Implementation
- **Models:** CalendarEvent, RestrictionInfo
- **ViewModels:** TodayViewModel, MonthViewModel, EventDetailViewModel with observable collections
- **CQRS:** ICommandBus / IQueryBus and handlers for commands and queries
- **Services:** JsonHairTransplantEventGenerator, CalendarDataInitializer

## Accessibility & Performance
- Ensure high contrast for event indicators
- Virtualized `CollectionView` for efficient rendering
- Use `Dispatcher` for UI updates on the main thread
- Cache query results to reduce redundant data access

## Platform Considerations
- Suppress `CollectionView` flicker on Android/iOS via `SelectionHighlightColor` and `SelectionChangedAnimationEnabled` when available
- Support `SafeAreaInsets` on iOS to avoid Dynamic Island overlap

---
*(Конец краткого обзора модуля Calendar)* 