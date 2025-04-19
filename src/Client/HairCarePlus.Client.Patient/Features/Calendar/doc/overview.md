# Модуль Calendar — обзор

> Версия: апрель 2025

## Назначение
Модуль Calendar отвечает за планирование и сопровождение пост‑операционного периода пациента. Он обеспечивает:
* генерацию базового расписания процедур на год вперёд;
* отображение событий в форматах Today, Month и Detail;
* отслеживание выполнения, перенос и завершение событий;
* локальное хранение и офлайн‑работу с последующей синхронизацией.

## Архитектура (Clean Architecture)
```
Calendar/
├── Domain/
│   ├── Entities/ HairTransplantEvent
│   ├── Repositories/ IHairTransplantEventRepository
│   └── Services/   Business policies (validators, generators)
├── Infrastructure/
│   └── Repositories/ HairTransplantEventRepository (EF Core)
├── Services/
│   ├── IDataInitializer (CalendarDataInitializer)
│   ├── IHairTransplantEventGenerator (JsonHairTransplantEventGenerator)
│   └── IRestrictionService / INotificationService (кросс‑компонентные)
├── ViewModels/ TodayViewModel, MonthViewModel, EventDetailViewModel
├── Views/ TodayPage, MonthPage, EventDetailPage
└── Helpers / Converters / Behaviors
```
Зависимости направлены строго «внутрь»: UI → ViewModel → Domain → Infrastructure.

## Поток данных `TodayPage`
1. `TodayPage` (View) триггерит `LoadCalendarDays()` в `TodayViewModel` при `OnAppearing`.
2. ViewModel обращается к `IHairTransplantEventService`, который в свою очередь использует `IHairTransplantEventRepository`.
3. Если это первый запуск, `CalendarDataInitializer` проверяет `Preferences` и вызывает генератор событий.
4. События кэшируются в слое Service, повторные запросы обслуживаются из памяти.

## DI‑регистрация (`CalendarServiceExtensions`)
```csharp
services.AddScoped<IHairTransplantEventRepository, HairTransplantEventRepository>()
        .AddScoped<IHairTransplantEventService, HairTransplantEventService>()
        .AddSingleton<IHairTransplantEventGenerator, JsonHairTransplantEventGenerator>()
        .AddScoped<IDataInitializer, CalendarDataInitializer>()
        .AddTransient<TodayViewModel>()
        .AddTransient<TodayPage>();
```

## логирование
* Структурированное через `ILogger<>`.
* Категория `HairCarePlus.Client.Patient.Features.Calendar` фильтруется на уровень `Information`.
* EF Core — `Warning` и выше.

## Синхронизация
`CalendarSyncService` (road‑map):
* инкрементальная выгрузка изменённых событий;
* разрешение конфликтов по `UpdatedAt`.

## UX‑паттерны
* Горизонтальный `CollectionView` со state‑driven UI (VisualStateManager).
* «Прыгучий» свайп сглажен (SnapPointsType = None, деликатная задержка).
* Анимация тапа через `VisualFeedbackBehavior` с `Easing.CubicOut/ SpringOut`.

## Расширение функционала
| Задача | Интерфейс | Реализация |
|--------|-----------|------------|
| Push‑напоминания | `INotificationService` | `NotificationService` (Infrastructure) |
| Синхронизация | `ICalendarSyncService` | `CalendarSyncService` |
| Генерация расписания клиник | `IHairTransplantEventGenerator` | `JsonHairTransplantEventGenerator` / `AI‑Planner` |

## Тестирование
* Юнит‑тесты: генератор, репозиторий (in‑memory Db), ViewModel логика.
* UI‑тесты: сценарии выбора дат, отметка выполнения.

## Риски
* Громоздкие json‑файлы расписания —→ использовать компрессию.
* Увеличение базы > 10k событий —→ требуется пагинация и lazy‑load.

---
*Документ поддерживается командой Calendar Feature. Изменения в API/архитектуре отражайте здесь.* 