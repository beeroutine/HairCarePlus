# TodayPage - Документация

## Назначение
TodayPage - основная страница календаря в приложении HairCare+ для пациентов, отображающая события текущего дня и позволяющая навигацию по дням в горизонтальном календаре.

## Ключевые функции
- Отображение текущей даты и количества дней после трансплантации
- Горизонтальный календарь для быстрого переключения между датами
- Визуальный индикатор выбранной даты с использованием VisualStateManager
- Отображение списка событий для выбранной даты
- Визуальные индикаторы различных типов событий (лекарства, фото, видео)
- Завершение задачи двумя жестами: **свайп влево** _или_ **долгое нажатие&nbsp;≥ 2 с** на карточке (реализовано через `SwipeView` и `TouchBehavior`).
- Анимированное кольцо-прогресс вокруг сегодняшней даты (Stroke + StrokeDashArray). Логика вычисления `CompletionProgress` добавлена во ViewModel.
- Возможность перейти к сегодняшней дате простым тапом по кругу.
- Адаптивный дизайн с поддержкой светлой/темной темы
- Сохранение выбранной даты между сеансами приложения

## Структура UI
### Заголовок (Header)
- Крупный заголовок с текущей датой (`FormattedTodayDate`)
- Дополнительная информация о днях после процедуры (`DaysSinceTransplant`)

### Горизонтальный календарь
- Реализован через `CollectionView` с горизонтальным `LinearItemsLayout`
- Элементы календаря:
  - День недели (верхняя метка)
  - Число месяца (в круглой рамке)
- Выбранная дата визуально выделяется через VisualStateManager

### Список событий
- Вертикальный `CollectionView`.
- Шаблон карточки построен на `Border` (вместо `Frame`), включает:
  - Цветную иконку типа события
  - Время, название, описание (до 2 строк)  
  - Состояние «выполнено» — заштрихованный фон + strikethrough текста.
- Жесты
  - Свайп-влево (SwipeItem → `ToggleEventCompletionCommand`)
  - Long-press ≥ 2 с (`TouchBehavior.LongPressCommand`).
- При отсутствии событий отображается EmptyView.

## Реализация
### MVVM Паттерн
- View: TodayPage.xaml - определяет пользовательский интерфейс
- ViewModel: TodayViewModel - управляет данными и логикой
- Привязка данных через `x:DataType` для компиляционных проверок

### Модель данных (TodayViewModel)
- Наследуется от BaseViewModel для поддержки уведомлений об изменении свойств
- Хранит и управляет:
  - Выбранной датой `SelectedDate`
  - Коллекцией дат для горизонтального календаря `CalendarDays`
  - Списком событий выбранного дня `FlattenedEvents`
  - Счетчиками событий по типам для визуальных индикаторов `EventCountsByDate`
  - Информацией о количестве дней после трансплантации `DaysSinceTransplant`

### Команды
- `SelectDateCommand` - обрабатывает выбор даты в горизонтальном календаре
- `ToggleEventCompletionCommand` - переключает статус выполнения события
- `OpenMonthCalendarCommand` - открывает полноэкранный месячный календарь
- `ViewEventDetailsCommand` - показывает детали выбранного события
- `PostponeEventCommand` - позволяет отложить событие на другую дату

### Обработка событий
Вся логика выбора и прокрутки теперь находится во ViewModel:
* `SelectDateCommand` — устанавливает `SelectedDate`, вызывает `LoadTodayEventsAsync`.
* Свойство `ScrollToIndexTarget` уведомляет View о необходимости прокрутки к дате.
* Визуальное состояние элементов даты управляется VisualStateManager автоматически, без кода-behind.

### Адаптивность и темы
Используется `AppThemeBinding`, все основные цвета вынесены в `Resources/Styles/Colors.xaml`.
Для светлой/тёмной темы предусмотрены альтернативные ключи (`Primary`, `PrimaryLight`, `PrimaryDark`).
Элементы выравниваются относительно `SafeAreaInsets` (iOS), что исключает перекрытие Dynamic Island.

## Текущий статус и дальнейший рефакторинг
### Исторический подход (конвертеры)
Ранее для выделения выбранной даты использовались следующие конвертеры:
- `DateToSelectionColorConverter` - определял цвет фона элемента в зависимости от выбранной даты
- `DateToBorderColorConverter` - определял цвет границы элемента
- `DateToTextColorConverter` - менял цвет текста для выделения выбранного дня
- `DateToIsSelectedConverter` - определял масштаб (увеличение) выбранного элемента

Проблемы старого подхода:
- Избыточные вычисления при каждой перерисовке UI
- Сложности с обновлением визуального состояния при программной смене даты
- Необходимость передавать параметры в конвертеры через BindingContext
- Потенциальные проблемы с производительностью при большом количестве элементов
- Сложное отслеживание и отладка проблем с визуальным состоянием

### Текущий подход (VisualStateManager)
Преимущества новой реализации:
- Использование нативного механизма выделения в CollectionView (`SelectionMode="Single"`)
- Декларативное определение визуальных состояний через VisualStateManager
- Автоматическое обновление состояния при смене выбранного элемента
- Улучшенная производительность за счет оптимизаций платформы
- Более чистый и поддерживаемый код без избыточных конвертеров
- Соответствие современным практикам разработки в MAUI

Реализация:
```xml
<VisualStateManager.VisualStateGroups>
    <VisualStateGroup Name="CommonStates">
        <VisualState Name="Normal" />
        <VisualState Name="Selected">
            <VisualState.Setters>
                <Setter Property="Frame.BackgroundColor" 
                        TargetName="dayFrame" 
                        Value="{AppThemeBinding Light={StaticResource Primary}, Dark={StaticResource Primary}}" />
                <Setter Property="Frame.BorderColor" 
                        TargetName="dayFrame" 
                        Value="Transparent" />
                <Setter Property="Label.TextColor" 
                        TargetName="dayText" 
                        Value="{StaticResource White}" />
                <Setter Property="Grid.Scale" Value="1.05" />
            </VisualState.Setters>
        </VisualState>
    </VisualStateGroup>
</VisualStateManager.VisualStateGroups>
```

## Синхронизация данных
- Загрузка событий при выборе даты через `LoadTodayEventsAsync`
- Обновление счетчиков событий для видимых дней через `LoadEventCountsForVisibleDaysAsync`
- Проверка просроченных событий через `CheckOverdueEventsAsync`
- Логирование загруженных событий для отладки
- Сохранение и восстановление выбранной даты через локальное хранилище (`SelectedDateKey`)

## Взаимодействие с сервисами
- `ICalendarService` - основной сервис для получения данных о событиях:
  - `GetEventsForDateAsync` - загрузка событий для выбранной даты
  - `GetEventsForDateRangeAsync` - загрузка событий для диапазона дат
  - `GetOverdueEventsAsync` - получение просроченных событий
  - `ToggleEventCompletionAsync` - изменение статуса выполнения события
  - `PostponeEventAsync` - перенос события на другую дату

## Производительность
- Использование `Dispatcher.DispatchAsync` для обновления UI из фоновых потоков
- Отложенная загрузка данных через `Task.Run`
- Кэширование данных о событиях для видимого диапазона дат
- Раздельное обновление различных частей интерфейса

## Хронология изменений

### Сентябрь 2025
* Переведены карточки событий c `Frame` на `Border` (улучшение производительности и стилизации).  
* Добавлено кольцо-прогресс вокруг текущей даты; вычисление `CompletionProgress` перенесено в ViewModel.  
* Реализован long-press ≥ 2 с через MAUI CommunityToolkit `TouchBehavior` (отказ от кастомного `LongPressGestureRecognizer`).  
* Цвет `PrimaryLight` добавлен в ресурсы; исправлены XAML-парсинг ошибки при запуске.

### Апрель 2025
* Все вызовы `Debug.WriteLine` в `TodayPage`, `TodayViewModel`, **VisualFeedbackBehavior** и связанных конвертерах удалены.
* Используется `ILogger<T>` из Microsoft.Extensions.Logging, регистрируемый через DI.
* Логи фильтруются в `MauiProgram.cs`:
  ```csharp
  builder.Logging.AddDebug()
               .AddFilter("HairCarePlus.Client.Patient", LogLevel.Information)
               .AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
  ```
* Исправлен «рывок» при горизонтальном свайпе календаря путём изменения `SnapPointsType="None"` и добавления задержки **100 мс** в методе `HandleScrollToIndexTargetChanged`.
* Выделение текущего дня гарантируется повторным применением `VisualState` после прокрутки.
* Поведение для анимации нажатия переведено на `ILogger`.
* Микро‑анимация использует `Easing.CubicOut` → `Easing.SpringOut`, что делает отклик более «живым».
* Первичная генерация календаря (≈1 226 записей) выполняется асинхронно при первом запуске.
* Кэш событий для видимых дат: hit‑rate вырос до >80 % при типичном сценарии навигации.
* `EnableSensitiveDataLogging()` активен только в `#if DEBUG`.
* В Release‑сборке чувствительные данные не логируются.

### Обновления (июль 2025)

1. Переход на SDK .NET 9.0.100‑rc.2 и MAUI 9.0+.
   * Исправлен системный серый фон `SelectedBackgroundView` на iOS – больше не требуется кастомный handler.
   * `DateSelectorView` снова использует штатный `SelectionMode="Single"` и двустороннюю привязку `SelectedItem ↔ SelectedDate`.

2. Центрирование на сегодняшней дате при запуске.
   * `TodayViewModel.ScrollToIndexTarget` устанавливается в `SelectedDate`, а `TodayPage` ― обрабатывает событие и скроллит `CollectionView` к центру.
   * Команда `GoToTodayCommand` привязана к круглому индикатору даты и сбрасывает выбор к текущему дню.

3. Удалена временная обработка `DateSelectorView_SelectionChanged` из кода‑behind ‑ вся логика выделения и прокрутки теперь в ViewModel + VisualStateManager.

4. Документация обновлена: описан фикс серого оверлея, актуальные версии SDK и упрощённый механизм выделения.

--- 