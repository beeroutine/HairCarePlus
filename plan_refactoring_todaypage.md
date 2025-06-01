# 🚀 План рефакторинга TodayPage для улучшения производительности

## 📋 Обзор проблем
- Множественные обновления UI при изменении даты
- Пересоздание коллекций вместо обновления
- Неэффективная работа с Messenger
- Конфликты анимаций
- Проблемы с виртуализацией CollectionView

## 📅 Этапы рефакторинга

### Этап 1: Оптимизация обновлений свойств (1-2 дня)

#### 🎯 Цель
Уменьшить количество вызовов OnPropertyChanged и перерисовок UI

#### 📝 Шаги

**1.1 Создать группировку обновлений свойств**

```csharp
// TodayViewModel.cs
private bool _isUpdatingDateProperties;

private void BatchUpdateDateProperties(Action updateAction)
{
    _isUpdatingDateProperties = true;
    updateAction();
    _isUpdatingDateProperties = false;
    
    // Одно обновление вместо множественных
    OnPropertyChanged(nameof(DateDisplayProperties));
}

// Новое computed свойство
public DateDisplayInfo DateDisplayProperties => new DateDisplayInfo
{
    FormattedSelectedDate = SelectedDate.ToString("ddd, MMM d"),
    CurrentMonthName = VisibleDate.ToString("MMMM"),
    CurrentYear = VisibleDate.ToString("yyyy"),
    DaysSinceTransplant = (SelectedDate.Date - _profileService.SurgeryDate.Date).Days + 1,
    DaysSinceTransplantSubtitle = $"Day {DaysSinceTransplant} post hair transplant"
};
```

**1.2 Оптимизировать setter SelectedDate**

```csharp
public DateTime SelectedDate
{
    get => _selectedDate;
    set
    {
        if (SetProperty(ref _selectedDate, value))
        {
            BatchUpdateDateProperties(() =>
            {
                if (value.Month != VisibleDate.Month || value.Year != VisibleDate.Year)
                {
                    VisibleDate = value;
                }
            });
            
            // Дебаунс для загрузки событий
            _dateChangeDebouncer.Debounce(300, async () =>
            {
                await LoadTodayEventsAsync();
                SaveSelectedDate(value);
            });
        }
    }
}
```

### Этап 2: Оптимизация работы с коллекциями (2-3 дня)

#### 🎯 Цель
Избежать пересоздания коллекций, использовать DiffUtil паттерн

#### 📝 Шаги

**2.1 Создать CollectionUpdater хелпер**

```csharp
// Helpers/CollectionUpdater.cs
public static class CollectionUpdater
{
    public static void UpdateCollection<T>(
        ObservableCollection<T> target, 
        IEnumerable<T> source,
        Func<T, T, bool> comparer)
    {
        var sourceList = source.ToList();
        
        // Удаляем элементы, которых нет в source
        for (int i = target.Count - 1; i >= 0; i--)
        {
            if (!sourceList.Any(s => comparer(s, target[i])))
            {
                target.RemoveAt(i);
            }
        }
        
        // Добавляем новые элементы
        foreach (var item in sourceList)
        {
            if (!target.Any(t => comparer(item, t)))
            {
                target.Add(item);
            }
        }
        
        // Сортируем, если нужно
        target.Sort(comparer);
    }
}
```

**2.2 Обновить UpdateUIWithEvents**

```csharp
private async Task UpdateUIWithEvents(IEnumerable<CalendarEvent> events, CancellationToken cancellationToken)
{
    if (cancellationToken.IsCancellationRequested) return;

    await MainThread.InvokeOnMainThreadAsync(() =>
    {
        var eventsList = events?.ToList() ?? new List<CalendarEvent>();
        var visibleEvents = eventsList.Where(e => !e.IsCompleted && e.EventType != EventType.CriticalWarning).ToList();

        // Обновляем без пересоздания
        CollectionUpdater.UpdateCollection(
            FlattenedEvents, 
            visibleEvents, 
            (a, b) => a.Id == b.Id);
        
        CollectionUpdater.UpdateCollection(
            SortedEvents,
            visibleEvents.OrderBy(e => e.Date.TimeOfDay),
            (a, b) => a.Id == b.Id);

        // Обновляем прогресс
        var (prog, percent) = _progressCalculator.CalculateProgress(eventsList);
        if (Math.Abs(CompletionProgress - prog) > 0.01)
        {
            CompletionProgress = prog;
            CompletionPercentage = percent;
        }
    });
}
```

### Этап 3: Оптимизация Messenger (1 день)

#### 🎯 Цель
Избежать полной перезагрузки при обновлении одного события

#### 📝 Шаги

**3.1 Создать умный обработчик сообщений**

```csharp
// В конструкторе TodayViewModel
_messenger.Register<EventUpdatedMessage>(this, async (recipient, message) =>
{
    await MainThread.InvokeOnMainThreadAsync(() =>
    {
        // Находим событие в коллекциях
        var eventToUpdate = FlattenedEvents.FirstOrDefault(e => e.Id == message.EventId);
        if (eventToUpdate != null)
        {
            // Если событие стало completed, удаляем из видимых
            if (message.IsCompleted)
            {
                FlattenedEvents.Remove(eventToUpdate);
                SortedEvents.Remove(eventToUpdate);
                EventsForSelectedDate.Remove(eventToUpdate);
            }
            else
            {
                // Обновляем свойства
                eventToUpdate.IsCompleted = message.IsCompleted;
            }
            
            // Обновляем только прогресс
            RecalculateProgress();
        }
        else if (!message.IsCompleted && message.Date == SelectedDate)
        {
            // Новое невыполненное событие для текущей даты
            await LoadSingleEventAsync(message.EventId);
        }
    });
});
```

### Этап 4: Замена горизонтального CollectionView (3-4 дня)

#### 🎯 Цель
Устранить проблемы производительности на Android

#### 📝 Шаги

**4.1 Создать кастомный DateSelector контрол**

```csharp
// Controls/HorizontalDateSelector.cs
public class HorizontalDateSelector : ScrollView
{
    private readonly StackLayout _container;
    private readonly Dictionary<DateTime, DateItemView> _dateViews;
    
    public static readonly BindableProperty SelectedDateProperty = 
        BindableProperty.Create(
            nameof(SelectedDate), 
            typeof(DateTime), 
            typeof(HorizontalDateSelector),
            DateTime.Today);
    
    public HorizontalDateSelector()
    {
        Orientation = ScrollOrientation.Horizontal;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Never;
        
        _container = new StackLayout 
        { 
            Orientation = StackOrientation.Horizontal,
            Spacing = 6
        };
        
        Content = _container;
        _dateViews = new Dictionary<DateTime, DateItemView>();
    }
    
    public void LoadDates(IEnumerable<DateTime> dates)
    {
        _container.Children.Clear();
        _dateViews.Clear();
        
        foreach (var date in dates)
        {
            var dateView = new DateItemView(date);
            dateView.Tapped += OnDateTapped;
            
            _container.Children.Add(dateView);
            _dateViews[date] = dateView;
        }
    }
}
```

**4.2 Обновить XAML**

```xml
<!-- Заменить CollectionView на кастомный контрол -->
<controls:HorizontalDateSelector 
    Grid.Row="1"
    x:Name="DateSelector"
    SelectedDate="{Binding SelectedDate, Mode=TwoWay}"
    HeightRequest="80"
    Margin="0,0,0,10" />
```

### Этап 5: Оптимизация анимаций (1-2 дня)

#### 🎯 Цель
Устранить конфликты между VisualStateManager и TouchBehavior

#### 📝 Шаги

**5.1 Выбрать единую систему анимаций**

```xml
<!-- Оставляем только TouchBehavior -->
<Border x:Name="rootCard">
    <Border.Behaviors>
        <toolkit:TouchBehavior
            LongPressCommand="{Binding Path=BindingContext.ToggleEventCompletionCommand, Source={x:Reference TodayPageRoot}}"
            LongPressCommandParameter="{Binding .}"
            PressedScale="0.95"
            PressedOpacity="0.8"
            DefaultAnimationDuration="150"
            DefaultAnimationEasing="{x:Static Easing.CubicOut}" />
    </Border.Behaviors>
    <!-- Убираем VisualStateManager -->
</Border>
```

### Этап 6: Добавление индикаторов загрузки (1 день)

#### 🎯 Цель
Улучшить восприятие производительности

#### 📝 Шаги

**6.1 Создать SkeletonView для событий**

```xml
<!-- Resources -->
<DataTemplate x:Key="EventSkeletonTemplate">
    <Border Style="{StaticResource CalendarCardBorderStyle}"
            BackgroundColor="{AppThemeBinding Light=#F0F0F0, Dark=#2A2A2A}">
        <Grid ColumnDefinitions="Auto,*" ColumnSpacing="16">
            <BoxView Grid.Column="0" 
                     WidthRequest="44" 
                     HeightRequest="44"
                     CornerRadius="22"
                     BackgroundColor="{AppThemeBinding Light=#E0E0E0, Dark=#3A3A3A}" />
            <StackLayout Grid.Column="1" Spacing="8">
                <BoxView HeightRequest="16" 
                         WidthRequest="200"
                         BackgroundColor="{AppThemeBinding Light=#E0E0E0, Dark=#3A3A3A}" />
                <BoxView HeightRequest="12" 
                         WidthRequest="150"
                         BackgroundColor="{AppThemeBinding Light=#E0E0E0, Dark=#3A3A3A}" />
            </StackLayout>
        </Grid>
    </Border>
</DataTemplate>
```

### Этап 7: Мониторинг и метрики (1-2 дня)

#### 🎯 Цель
Измерить улучшения производительности

#### 📝 Шаги

**7.1 Добавить Performance Monitor**

```csharp
public class PerformanceMonitor
{
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly Dictionary<string, Stopwatch> _timers = new();
    
    public void StartTimer(string operation)
    {
        _timers[operation] = Stopwatch.StartNew();
    }
    
    public void StopTimer(string operation)
    {
        if (_timers.TryGetValue(operation, out var timer))
        {
            timer.Stop();
            _logger.LogInformation("Operation {Operation} took {ElapsedMs}ms", 
                operation, timer.ElapsedMilliseconds);
            
            if (timer.ElapsedMilliseconds > 100)
            {
                _logger.LogWarning("Slow operation detected: {Operation}", operation);
            }
        }
    }
}
```

## 📊 Ожидаемые результаты

### Метрики производительности
- ⚡ Время отклика UI: < 16ms (60 FPS)
- 📱 Использование памяти: -30%
- 🔄 Время загрузки событий: -50%
- 📊 Плавность анимаций: 60 FPS на всех платформах

### Пользовательский опыт
- ✅ Мгновенный отклик на жесты
- ✅ Плавная прокрутка без рывков
- ✅ Быстрое переключение между датами
- ✅ Отсутствие блокировок UI

## 🛠️ Инструменты для тестирования

1. **Профайлер .NET MAUI**
   ```bash
   dotnet trace collect --process-id <PID> --providers Microsoft-Maui
   ```

2. **UI Performance Analyzer**
   - Измерение FPS
   - Анализ jank frames
   - GPU overdraw

3. **Memory Profiler**
   - Отслеживание утечек памяти
   - Анализ аллокаций

## 📝 Чек-лист для ревью

- [ ] Все тесты проходят
- [ ] Производительность измерена на целевых устройствах
- [ ] Код соответствует SOLID принципам
- [ ] Документация обновлена
- [ ] Нет регрессий в функциональности

## 🎯 Следующие шаги

После завершения рефакторинга TodayPage:
1. Применить аналогичные оптимизации к другим страницам
2. Создать guidelines по производительности для команды
3. Настроить автоматический мониторинг производительности в CI/CD 