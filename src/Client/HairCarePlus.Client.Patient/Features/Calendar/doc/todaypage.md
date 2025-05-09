# TodayPage - Документация

## Назначение
TodayPage - основная страница календаря в приложении HairCare+ для пациентов, отображающая события текущего дня и позволяющая навигацию по дням в горизонтальном календаре.

## Ключевые функции
- Отображение текущей даты и количества дней после трансплантации
- Горизонтальный календарь для быстрого переключения между датами
- Визуальный индикатор выбранной даты с использованием VisualStateManager
- Отображение списка событий для выбранной даты
- Визуальные индикаторы различных типов событий (лекарства, фото, видео)
- Цветовая индикация типов событий через `DataTrigger` и ресурсы цвета (без конвертера)
- Завершение задачи двумя жестами: **свайп влево** _или_ **долгое нажатие\u00a0≥ 2 с** на карточке (реализовано через `SwipeView` и `TouchBehavior`).
- Со стабильной MAUI 10 (начиная с preview 4) мерцание системы при выборе даты можно убрать нативными свойствами `SelectionHighlightColor` и `SelectionChangedAnimationEnabled` у `CollectionView`. В **preview 3** эти API ещё отсутствуют, поэтому пока продолжает использоваться VSM-workaround (см. DataTemplate + `CollectionViewSelectionStateBehavior`).
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
- все команды реализуются через **CQRS** (`ICommandBus`/`IQueryBus`) и тестируемы в isolation

### Обработка событий
Вся логика выбора и прокрутки теперь находится во ViewModel:
* `SelectDateCommand` — устанавливает `SelectedDate`, затем отправляет `GetEventsForDateQuery` через `IQueryBus`.
* Полученные события объединяются и раскидываются по коллекциям UI, далее `EventCounts` запрашиваются одной пачкой (`GetEventCountsForDatesQuery`).
* Смайпы/долгое нажатие отправляют `ToggleEventCompletionCommand` в `ICommandBus`; хендлер обновляет репозиторий и публикует `EventUpdatedMessage`, что триггерит обновление UI.
* Свойство `ScrollToIndexTarget` уведомляет View о необходимости прокрутки к дате.
* Визуальное состояние элементов даты управляется VisualStateManager — конвертеры не используются.

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
Работа с данными идёт через слой **Application/CQRS**: 
* Queries (`GetEventsForDateQuery`, `GetEventCountsForDatesQuery`, `GetActiveRestrictionsQuery`) 
* Commands (`ToggleEventCompletionCommand`, `PostponeEventCommand`)
Инфраструктурный `IHairTransplantEventRepository` обеспечивают кэш и persistance.

## Производительность (кратко)
* UI-обновления через `Dispatcher`.
* Хендлеры кэшируют данные и отправляют `EventUpdatedMessage` для точечных обновлений.

*(Для подробностей изменений см. CHANGELOG.md)*

--- 