# Модуль Calendar — обзор

> Версия: сентябрь 2025

# Важно: Проект использует .NET MAUI 9.0.51 SR (стабильная версия)

## Назначение
Модуль Calendar отвечает за планирование и сопровождение пост‑операционного периода пациента. Он обеспечивает:
* генерацию базового расписания процедур на год вперёд;
* отображение событий в форматах Today, Month и Detail;
* отслеживание выполнения, перенос и завершение событий;
* локальное хранение и офлайн‑работу с последующей синхронизацией.

## Архитектура (обновлённая Clean Architecture + CQRS)
```
Calendar/
├── Application/
│   ├── Commands/   # ToggleEventCompletionCommand …
│   ├── Queries/    # GetEventsForDateQuery, GetEventCountsForDatesQuery …
│   └── Messages/   # EventUpdatedMessage
├── Domain/
│   └── Entities/   # CalendarEvent, RestrictionInfo …
├── Infrastructure/
│   └── Repositories/ HairTransplantEventRepository (EF Core / cache)
├── Services/       # IDataInitializer, IHairTransplantEventGenerator …
├── ViewModels/     # TodayViewModel, MonthViewModel, EventDetailViewModel
└── Views/          # TodayPage, MonthPage, EventDetailPage
```
Зависимости:
UI → **Application (CQRS)** → Domain → Infrastructure.

## Поток данных `TodayPage`
1. `TodayPage` (View) триггерит `LoadCalendarDaysAsync()` в `TodayViewModel` при `OnAppearing`.
2. ViewModel запрашивает данные через **`IQueryBus`** → `GetEventsForDateQuery` / `GetEventCountsForDatesQuery`.
3. Пользовательские действия (свайп/лонг-пресс) отправляют **`ICommandBus`** → `ToggleEventCompletionCommand`.
4. Handlers применяют изменения к репозиторию и публикуют `EventUpdatedMessage`; ViewModel реагирует и обновляет UI.
5. При первом запуске `CalendarDataInitializer` и `IHairTransplantEventGenerator` заполняют базу событий.

## DI-регистрация (CalendarServiceExtensions)
```csharp
services.AddCqrs() // регистрирует InMemoryCommandBus / QueryBus
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

*(Полный список регистраций и подробности см. в `CalendarServiceExtensions.cs`)*

---
*(Конец краткого обзора модуля Calendar)* 