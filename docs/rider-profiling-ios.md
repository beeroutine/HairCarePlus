# 📱 Профилирование HairCarePlus iOS в Rider

## Настройка профилирования для iOS

### 1. Создание конфигурации для профилирования

1. В вашем окне **Run/Debug Configurations** нажмите `+`
2. Выберите `.NET Project`
3. Настройте:
   ```
   Name: HairCarePlus iOS Profile
   Project: src\Client\HairCarePlus.Client.Patient
   Platform type: iOS
   Configuration: Release (важно для точных метрик!)
   ```

### 2. Дополнительные настройки для iOS

В разделе **Extra mlaunch arguments** добавьте:
```
--profiler --enable-background-fetch
```

### 3. Запуск профилирования

#### Способ A: Через меню
1. Выберите конфигурацию "HairCarePlus iOS Profile"
2. Нажмите правой кнопкой на кнопке Run
3. Выберите `Profile with → dotTrace`

#### Способ B: Через горячие клавиши
- macOS: `Cmd+Shift+Alt+F11`
- Windows/Linux: `Ctrl+Shift+Alt+F11`

### 4. Выбор типа профилирования

При запуске появится диалог. Выберите:

#### Для первого анализа TodayPage:
```
Profiling type: Timeline
✅ Collect call stacks every: 1 ms
✅ Collect thread times
✅ Collect memory traffic
✅ Collect native memory allocations
```

#### Для анализа производительности:
```
Profiling type: Sampling
Measure: Wall time (CPU and wait time)
Time measurement accuracy: High
```

### 5. Тестовый сценарий для TodayPage

После запуска профилирования:

1. **Дождитесь загрузки приложения**
2. **Начните запись** (Start Recording в окне dotTrace)
3. **Выполните действия:**
   - Откройте TodayPage
   - Переключите 5-10 дат подряд
   - Прокрутите список событий
   - Выполните long-press на 3-4 карточках
   - Отметьте несколько задач выполненными
4. **Остановите запись** (Stop Recording)

### 6. Анализ результатов для iOS

#### Особенности iOS профилирования:

1. **Main Thread использование**
   - iOS очень чувствителен к блокировкам Main Thread
   - Любая операция > 16ms вызовет визуальные лаги

2. **Memory Pressure**
   - iOS агрессивно управляет памятью
   - Проверьте Memory Traffic в Timeline

3. **CollectionView Performance**
   - Особое внимание к `CalendarDays` горизонтальному скроллу
   - Проверьте Cell Reuse эффективность

### 7. Что искать в результатах

#### Call Tree:
```
TodayViewModel
├── LoadTodayEventsAsync [< 50ms на iOS] ✅
├── UpdateUIWithEvents [< 8ms] ✅
├── PreloadingService.PreloadAdjacentDatesAsync [фоновый поток] ✅
└── ConfettiManager.ShowConfettiAsync [< 30% CPU] ✅
```

#### Hot Spots для iOS:
- 🔴 `UICollectionView` layout вычисления
- 🔴 `SKConfettiView` рендеринг
- 🟡 `ObservableCollection` уведомления
- 🟢 Кэширование событий

### 8. Типичные проблемы iOS

#### Проблема: "No profiling data collected"
```bash
# Решение 1: Используйте физическое устройство
# Симулятор может не поддерживать все виды профилирования

# Решение 2: Проверьте Provisioning Profile
# Должен быть Development, не Distribution
```

#### Проблема: Высокое использование памяти
```csharp
// Проверьте в профайлере:
// 1. Утечки в event handlers
// 2. Циклические ссылки в ViewModels
// 3. Незакрытые подписки на Messenger
```

### 9. Оптимизация для iOS

После анализа результатов:

1. **Если Main Thread > 90%:**
   - Переместите тяжелые операции в фоновые потоки
   - Используйте `Task.Run` для вычислений

2. **Если много GC:**
   - Уменьшите аллокации в `UpdateUIWithEvents`
   - Используйте object pooling для частых объектов

3. **Если лагает скролл:**
   - Проверьте сложность layout в CollectionView
   - Упростите DataTemplate

### 10. Экспорт результатов

1. В окне dotTrace: `File → Export → Performance Report`
2. Выберите:
   - Format: HTML Report
   - Include: Call Tree, Hot Spots, Timeline
3. Поделитесь с командой для code review 