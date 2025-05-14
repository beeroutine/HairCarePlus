# Progress – страница восстановления

Версия спецификации: май 2025

## Назначение
Показывает пациенту краткий статус ограничений после операции и ленту всех сделанных фото-отчётов.

* Верхняя полоса – **Restriction Timers**. Кружки с количеством дней до окончания ограничений (спорт, алкоголь и т.д.).
* Основная зона – вертикальный **CarouselView** (instagram-style). Каждый слайд – локальный снимок + AI-оценка.

## Стек
* .NET MAUI 9.0.51 SR
* MVVM (CommunityToolkit.Mvvm)
* Clean Architecture – UI ◇ Application ◇ Domain

## Структура модуля
```
Progress/
 ├─ Domain/
 │   └─ Entities/
 │       ├─ ProgressPhoto.cs
 │       └─ RestrictionTimer.cs
 ├─ ViewModels/
 │   └─ ProgressViewModel.cs
 ├─ Views/
 │   ├─ ProgressPage.xaml(+ .cs)
 │   └─ Styles/ProgressStyles.xaml
 ├─ Application/
 │   └─ Queries/GetProgressPhotosQuery.cs (зарезервировано)
 └─ doc/progress.md
```

## UI-детали
### Restriction Circles
* `CollectionView` horizontal, item spacing 12.
* Круг 60×60 px, stroke `Primary`. Внутри – оставшиеся дни (`DaysRemaining`).
* Под кругом подпись ограничения (`Title`).

### Photo Carousel
* `CarouselView` orientation VerticalList.
* `Border` со скруглением 12 и высотой ~350 px.
* `Image` `AspectFill`, источник – `LocalPath`.
* В перспективе поверх будет накладываться индикатор `AiScore` и кнопка «удалить».

## Потоки данных
1. `ProgressViewModel` при создании:
   * Загружает ограничения из сервиса `IRestrictionService` (пока заглушка).
   * Сканирует каталог `[…] /Library/Caches/HairCarePlus/captured_photos/` на устройстве и формирует `ObservableCollection<ProgressPhoto>`.
2. При появлении новых снимков (через Messenger или повторный вход) VM пересканирует директорию.

## Расширение
* После интеграции AI backend `AiScore` и `AiReport` заполняются через SignalR-сообщение `photoReportReady`.
* Лонг-тап на фото открывает полноэкранный просмотр + markdown-отчёт.

---
© HairCare+, 2025

## v2 Redesign — Minimalism First

### 1. Суть страницы
* Одним взглядом ответить пациенту на два вопроса:  
  1. *«Как идёт моё восстановление сегодня?»*  
  2. *«Что ещё под запретом и когда закончится?»*
* Минимум отвлекающих элементов: плоский UI, до двух акцентных цветов.

### 2. Компоненты UI
|Блок|Описание|
|----|---------|
|Restriction Circles|Горизонтальный `CollectionView`, отображающий все активные ограничения в виде кругов. Внутри каждого круга – количество оставшихся дней. Если количество ограничений превышает видимую область, обеспечивается возможность просмотра всех элементов (например, через горизонтальную прокрутку с явным индикатором или специальный элемент "+N" / "Показать все"). Цвета кругов: `Primary` (активно) / `SurfaceVariant` (< 24 ч) / `Surface` (снято). Tap по кругу → всплывающее окно с подробным описанием ограничения.|
|Diary Feed|Вертикальная, интуитивно понятная лента постов в стиле Instagram, отсортированная по дате (последние посты вверху). Каждый элемент ленты представляет один день и содержит: заголовок (например, "День 1" или дата), основные фото-отчеты дня (1-3 ключевых изображения), текстовое описание/авто-заметку, и метку AI-анализа (например, «OK», «Attention», или AI Score) или отчет доктора. Tap по AI-метке или специальной кнопке на посте → открытие Bottom-Sheet Insights.|
|Bottom-Sheet Insights|Ленивая подгрузка (`CommunityToolkit.Maui.Popup`). Графики роста/покраснения, % выполнения процедур, кнопка «Share with Clinic».|

### 3. Потоки данных / CQRS
* **Commands**  
  `CapturePhotoCommand` → локальное сохранение + `PhotoCapturedMessage`  
  `CompleteProcedureCommand` → отмечает чек-лист  
* **Queries**  
  `GetDailyProgressQuery` → агрегирует локальные фото, AI-report, процедуры и обеспечивает сортировку по убыванию даты (последние вверху)
  `GetRestrictionsQuery` → TTL ограничений

### 4. Доменные модели (draft)
```csharp
record RestrictionTimer(string Id, string Title, DateTime EndsAt);
record ProgressPhoto(string LocalPath, DateOnly Date, PhotoZone Zone);
record ProcedureCheck(string Id, bool IsDone, DateOnly Date);
record AIReport(DateOnly Date, AiScore Score, string Summary);
```

### 5. Технические заметки
* Для сравнения фото используется `SharedTransitionNavigationPage` (iOS/Android).  
* Локальный кэш фото: `[CacheDir]/HairCarePlus/captured_photos/`.  
* AI-отчёт приходит по SignalR `photoReportReady` и кешируется в SQLite.

---
© HairCare+, 2025 