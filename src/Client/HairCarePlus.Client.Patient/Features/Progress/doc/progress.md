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