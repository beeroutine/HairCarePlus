# Progress Page — Redesign v4 (June 2025)

## Цель

Создать современную, минималистичную и визуально привлекательную страницу восстановления пациента, где **фото-прогресс** выходит на первый план, а **AI- и врачебные комментарии** подаются лаконично. Добавить обзорный **годовой таймлайн** и **ближайшие ограничения** в стиле Instagram-подобных интерактивных элементов.

## Общая схема

```
┌ Header (sticky) ─────────────────────────────────────────────────────┐
│  ●───▇▇▇───●───▇▇▇───●───▇▇▇───●  Year Timeline  0–1m 1–3m 3–6m 6–12m  │
│  ◉  Курение запрещено  ◯  Не наклонять  ◯  Нет спорта  ◯  Алкоголь запрещен │
└───────────────────────────────────────────────────────────────────────┘
┌ ProgressCardView (Day NN) ─────────────────────────────────────────────┐
│ ┌──────────────────────────────────────────────────────────────────┐ │
│ │                [ Фото (AspectFill) ]                           │ │
│ │     (Tap → Fullscreen with zoom & share)                       │ │
│ └──────────────────────────────────────────────────────────────────┘ │
│  Day NN           (дата: dd MMM yyyy)                            │
│  ───────────────────────────────────────────────────────────────  │
│  🤖 AI-анализ: Score 72                                       │
│     Doing well! Keep the area clean and dry.                   │
│  ───────────────────────────────────────────────────────────────  │
│  👩‍⚕️ Врач:                                    │
│     Плавно массировать область мягкими движениями.             │
└────────────────────────────────────────────────────────────────────┘
```

* **Sticky Header:** годовой таймлайн + ограничения всегда на виду.
* **Vertical Scroll:** привычная лента карточек.
* **Pull-to-Refresh:** `RefreshView` для обновления данных.
* **Floating Action Button:** кнопка «+ Фото» внизу справа для быстрого добавления снимка.

---

## 1 Header

### 1.1 Year Timeline (0–12 мес)

* **Толщина линии:** 3px (минималистично).
* **Пройденный путь:** заполненная часть линии `PrimaryColor`, оставшаяся — нейтральный `SurfaceVariant`.
* **Сегменты:** метки `1m`, `3m`, `6m`, `12m` под шкалой (Caption, 12sp).
* **Текущий день:** контрастный кружок (◉) на линии, с цифрой дня по тапу.
* **Tap on marker:** показывать popup «День N из 365» + статус.

### 1.2 Restriction Timers

* **CollectionView** (горизонтально): 2–4 элемента, >4 → прокрутка.
* **ItemTemplate:** круглая иконка + подпись (Caption, 12sp).

  * `Border` круглой формы, `PrimaryColor` — активные (>1d), `SurfaceVariant` — скоро (≤24h), `Surface` — завершено.
* **Иконки:** контурные (FA6 solid), внутри пиктограммы: 🚬, 🤸‍♂️, 🏋️‍♀️, 🍷.
* **Tap:** тултип с подробностями ограничения.

---

## 2 Timeline Feed (Vertical)

### 2.1 ProgressCardView

| Элемент      | Детали                                                         |
| ------------ | -------------------------------------------------------------- |
| **Фото**     | `CarouselView`: AspectFill, скругл. углы 12px;                 |
|              | **Tap** → Fullscreen модалка + pinch-zoom + share              |
| **Header**   | Overlay на фото: `Day NN` (18sp Bold, white)                   |
|              | Под фото: дата (Caption, 12sp, SecondaryColor)                 |
| **AI block** | `Border` 8px, фон `#F5F5F5`/`#2C2C2E`, Padding 8               |
|              | 🤖 `AI-анализ:` (13sp Semibold) + `Score` (16sp, PrimaryColor) |
| **Doctor**   | 👩‍⚕️ `Врач:` (13sp Semibold) + комментарий (14sp Regular)     |

* **Отступы:** Padding карточки 16px, между блоками 8px.
* **Тень:** subtle Shadow (Blur 4, Opacity 0.1) или тонкая обводка с прозрачностью.
* **MaxLines:** 3 для текста; **Tap** → full-screen sheet (анимация).

### 2.2 Поведение

* **Swipe** по фото внутри карточки.
* **Tap** по тексту → expand.

---

## 3 Взаимодействие

* **Pull-to-Refresh:** `RefreshView` вокруг `CollectionView`.
* **Sticky Header:** через `Shell.TitleView` или `StickyHeaderBehavior`.
* **FAB «+ Фото»:** `AbsoluteLayout` с `Border` (50×50, PrimaryColor).
* **Micro-interactions:** VisualStateManager для `Pressed`, анимация Scale.

---

## 4 Стилистика

* **Цвета:**

  * `BackgroundLight`=#FFFFFF, `BackgroundDark`=#121212
  * `CardBackgroundLight`=#F0F0F0, `CardBackgroundDark`=#1E1E1E
  * `PrimaryColor`=#3478F6, `ErrorColor`=#E94545, `SuccessColor`=#4CAF50
  * Текст: `TextPrimaryLight`=#222222, `TextSecondaryLight`=#666666;
    `TextPrimaryDark`=#F5F5F5, `TextSecondaryDark`=#A0A0A0
* **Шрифты:**

  * H1 (заголовок страницы) — 20sp Semibold
  * CardTitle (`Day NN`) — 18sp Bold
  * BodyText (комментарий) — 14–16sp Regular
  * Caption (метки) — 12sp Regular
* **Иконки:** FontAwesome 6 solid (контурные), размер 24px.
* **Отступы:** крайние — 16px, вложенные — 8–12px.
* **Темы:** поддержка Light/Dark через AppThemeBinding.

---

## 5 Data / CQRS Flow

| Trigger             | Query / Command              | Обновляет          |
| ------------------- | ---------------------------- | ------------------ |
| App start / Refresh | `GetProgressFeedQuery`       | Header + Feed      |
| New photo saved     | `PhotoCapturedMessage`       | Добавляет карточку |
| Restriction updated | `RestrictionsChangedMessage` | Timers Header      |
| Pull-to-Refresh     | оба запроса                  | Header + Feed      |

---

## 6 Детальный план реализации

### 6.1 Обновление дизайн-системы
- Актуализировать цвета в `Colors.xaml`:
  - PrimaryColor = #3478F6, SurfaceVariant = нейтральный фон для незавершённых, Surface = для завершённых
  - Добавить цвета карточек: CardBackgroundLight и CardBackgroundDark
- Обновить `ProgressStyles.xaml`:
  - Стили для карточек (CornerRadius=12, Shadow Blur=4, Opacity=0.1)
  - Стили для блоков AI и Doctor (BorderThickness=1, CornerRadius=8, Padding=8)
  - Шрифты: 18sp Bold для заголовка карточки, 12sp Caption, 14–16sp Regular для текста

### 6.2 Реализация кастомных компонентов

#### a) `YearTimelineView`
- Линия толщиной 3px, заполненная часть = PrimaryColor, остальная = SurfaceVariant
- Микросегменты: метки 1m, 3m, 6m, 12m (Caption, 12sp) под линией
- Текущий день: контрастный кружок (◉) на позиции дня, при тапе выводить popup «День N из 365» + статус
- Поддержка Light/Dark через AppThemeBinding

#### b) `RestrictionTimersView`
- Горизонтальный `CollectionView` с ItemSpacing=10, показывать первые 4 элемента, >4 → последний элемент шаблон "+N"
- `StandardRestrictionTimerTemplate`:
  - `Border` круглый, Stroke и Background в зависимости от `DaysRemaining` (Active >1d, Soon ≤1d, Completed)
  - Icon FontAwesome 24px, внутри 24×24
  - Label с `{DaysRemaining}d` (FontSize=11, MaxLines=2)
  - TapGesture → команда `OpenRestrictionDetailsCommand`
- `ShowMoreRestrictionsTemplate` для отображения оставшихся элементов:
  - Круглая рамка с текстом "+N"
  - TapGesture → `ShowAllRestrictionsCommand`

#### c) `ProgressCardView`
- `CarouselView` (AspectFill, CornerRadius=12px), поддержка свайпа фотографий
- Overlay заголовка `Day {DayNumber}` (FontSize=18sp, Bold, белый цвет) в левом верхнем углу фотографии
- Под фото: дата в формате `dd MMM yyyy` (Caption, 12sp, SecondaryColor)
- Блок AI:
  - `Border` толщиной 1px, CornerRadius=8, Background=#F5F5F5 / #2C2C2E, Padding=8
  - Текст: 🤖 AI-анализ: {Score} (13sp Semibold + Score 16sp PrimaryColor), комментарий не более 3 строк, при тапе разворачивается
- Блок Doctor:
  - Аналогичный блок: 👩‍⚕️ Врач: (13sp Semibold), текст 14sp Regular, MaxLines=3, тап для полного просмотра

### 6.3 Макет страницы
- `RefreshView` оборачивает `CollectionView` с Feed (Pull-to-Refresh через `LoadCommand`)
- `CollectionView.Header` используется для YearTimelineView и RestrictionTimersView (Sticky Header через `Shell.TitleView` или `StickyHeaderBehavior`)
- Добавить `Button`-FAB:
  - AbsoluteLayout, позиционирование (1,1), размер 50×50, CornerRadius=25, BackgroundColor=PrimaryColor, Text="+", FontSize=24
  - Команда `AddPhotoCommand`

### 6.4 Взаимодействие и анимации
- Pull-to-Refresh для всего списка
- Swipe для фотографий внутри карточки
- Tap по текстовым блокам (AI, Doctor) → full-screen sheet с анимацией
- VisualStateManager: эффект Scale=0.9 для нажатий на иконки ограничений и FAB

### 6.5 Темизация и доступность
- Все цвета и стили через `AppThemeBinding`
- Добавить `AutomationProperties.Name` и `IsInAccessibleTree` для элементов: FAB, иконок ограничений, фотографий

### 6.6 Производительность и тесты
- Использовать `CachedImage` (FFImageLoading или аналог) для фото
- Lazy loading данных в Feed
- Покрыть Unit-тестами ViewModel (`ProgressViewModel`) и Message Handlers (`PhotoCapturedMessageHandler`, `RestrictionsChangedMessageHandler`)

© HairCare+, 2025

---

## 7 Контроль ревью

- [x] Шаг 1: Перенос YearTimeline и RestrictionTimers в Shell.TitleView (ProgressPage.xaml)
- [x] Шаг 2: Проверить и обновить Colors.xaml и ProgressStyles.xaml
- [x] Шаг 3: Проверить адаптацию RestrictionTimersView под стили Progress
- [x] Шаг 4: Реализовать YearTimelineView
- [x] Шаг 5: Реализовать ProgressCardView
- [x] Шаг 6: Настроить взаимодействия и анимации (Swipe, Tap → full-screen)
- [x] Шаг 7: Добавить AutomationProperties и доступность
- [x] Шаг 8: Интегрировать CachedImage и lazy load
- [x] Шаг 9: Написать Unit-тесты для ViewModel и Message Handlers

## 8 Предложения и план рефакторинга

1. **Выделение интерфейсов для UI-компонентов**
   - Создать интерфейсы `IYearTimelineView`, `IProgressCardView` и `IRestrictionTimersView` для упрощения мокирования в тестах.
2. **Разделение Responsibilities**
   - Переместить логику отрисовки и обработки касаний из `YearTimelineView` в `TimelineService` (реализовано).
3. **Улучшение навигации**
   - Вынести навигационные команды (`AddPhotoCommand`, `PreviewPhotoCommand`) в `INavigationService` с реализацией через `Shell`.
4. **Унификация ресурсов**
   - Вынести константы шрифтов и отступов в отдельный файл `Dimensions.xaml`.
5. **Оптимизация производительности**
   - Добавить пул кеширования `CachedImage` для снижения нагрузки на память.
   - Lazy load исторических фото и ограничений при прокрутке.
6. **Расширение тестового покрытия**
   - Добавить UI-тесты для ключевых сценариев (OTTO) с использованием MAUI.Testing.

© HairCare+, 2025
