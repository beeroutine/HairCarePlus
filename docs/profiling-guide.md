# 📊 Руководство по профилированию HairCarePlus в Rider

## 🚀 Быстрый старт

### Предварительная настройка

1. **Установите необходимые инструменты в Rider:**
   - File → Settings → Plugins
   - Установите: dotTrace, dotMemory, Dynamic Program Analysis

2. **Подготовьте проект:**
   ```bash
   # Соберите в Release конфигурации
   dotnet build -c Release
   ```

## 📱 Профилирование .NET MAUI приложения

### Метод 1: Встроенный профайлер Rider

#### 1. Создание профиля запуска

1. Откройте `Run/Debug Configurations` (Edit Configurations...)
2. Нажмите `+` → `.NET Project`
3. Настройте:
   ```
   Name: HairCarePlus Profile
   Project: HairCarePlus.Client.Patient
   Target Framework: net9.0-android (или net9.0-ios)
   Configuration: Release
   ```

#### 2. Запуск профилирования

1. Выберите созданную конфигурацию
2. Нажмите иконку профилирования (◐) рядом с Run
3. Выберите тип профилирования:
   - **Timeline** - для общего анализа
   - **Sampling** - для быстрого обзора
   - **Tracing** - для детального анализа

### Метод 2: Профилирование запущенного приложения

#### Для Android:

```bash
# 1. Запустите приложение
dotnet build -t:Run -f net9.0-android -c Release

# 2. Найдите PID процесса
adb shell ps | grep haircare

# 3. В Rider: Run → Attach Profiler to Process
# Выберите процесс по PID
```

#### Для iOS:

```bash
# 1. Запустите на устройстве/симуляторе
dotnet build -t:Run -f net9.0-ios -c Release

# 2. В Rider: Run → Attach Profiler to Process
# Выберите HairCarePlus.Client.Patient
```

## 🎯 Сценарии профилирования TodayPage

### Сценарий 1: Загрузка событий

1. **Подготовка:**
   - Запустите профилирование
   - Откройте TodayPage
   - Начните запись (Start Recording)

2. **Действия:**
   - Переключитесь на другую дату
   - Прокрутите список событий
   - Отметьте несколько задач выполненными

3. **Анализ:**
   - Остановите запись
   - Найдите в Call Tree: `LoadTodayEventsAsync`
   - Проверьте время выполнения и аллокации

### Сценарий 2: Анимации и UI

1. **Настройка Timeline профилирования:**
   ```
   Profiling Type: Timeline
   ✅ Collect thread times
   ✅ Collect UI freeze events
   ```

2. **Тестирование:**
   - Быстро переключайте даты
   - Используйте long-press на карточках
   - Прокручивайте горизонтальный календарь

## 📈 Анализ результатов

### Основные метрики для TodayPage:

#### 1. Call Tree Analysis
```
LoadTodayEventsAsync
├── Time: < 100ms ✅
├── Allocations: < 1MB ✅
└── Database calls: < 50ms ✅

UpdateUIWithEvents  
├── Time: < 16ms (60 FPS) ✅
├── UI thread blocking: 0ms ✅
└── Collection updates: < 5ms ✅
```

#### 2. Hot Spots (проблемные места)
- 🔴 `CalendarDays` CollectionView прокрутка
- 🟡 `EventsForSelectedDate` обновление
- 🟢 `ToggleEventCompletionAsync` выполнение

### Ключевые показатели:

| Метрика | Целевое значение | Критическое |
|---------|-----------------|-------------|
| Frame time | < 16ms | > 33ms |
| Memory allocations/sec | < 1MB | > 5MB |
| GC pressure | < 5% | > 15% |
| UI thread blocking | 0ms | > 100ms |

## 🛠️ Оптимизация по результатам

### Если LoadTodayEventsAsync > 200ms:

1. **Проверьте кэширование:**
   ```csharp
   // В профайлере найдите:
   CalendarCacheService.TryGet // Должно быть > 80% hit rate
   ```

2. **Оптимизируйте запросы:**
   ```csharp
   // Ищите множественные вызовы:
   GetEventsForDateQuery // Должен быть 1 на дату
   ```

### Если UI лагает (< 60 FPS):

1. **Проверьте аллокации в UpdateUIWithEvents:**
   - CollectionUpdater должен переиспользовать объекты
   - Не должно быть создания новых ObservableCollection

2. **Анализируйте Timeline:**
   - Ищите красные полосы (UI freezes)
   - Проверьте Main Thread utilization

## 💡 Практические советы

### 1. Профилирование предзагрузки

```csharp
// Добавьте временные логи для анализа:
_performanceMonitor.StartTimer("PreloadAdjacentDates");
// ... код предзагрузки
_performanceMonitor.StopTimer("PreloadAdjacentDates");
```

### 2. Мониторинг конфетти анимации

В Timeline профайлере:
- Фильтр: `ConfettiManager`
- Проверьте CPU usage во время анимации
- Должно быть < 30% на одном ядре

### 3. Экспорт результатов

1. File → Export Performance Report
2. Формат: HTML для отчетов
3. Включите: Call Tree, Hot Spots, Timeline

## 🚨 Частые проблемы

### Проблема: Профайлер не подключается

```bash
# Android: включите отладку
adb shell setprop debug.mono.profile '127.0.0.1:9999'

# iOS: используйте физическое устройство
# Симулятор может давать неточные результаты
```

### Проблема: Нет данных о методах

1. Убедитесь в Release конфигурации
2. Проверьте Project Properties:
   ```xml
   <PropertyGroup Condition="'$(Configuration)'=='Release'">
     <DebugSymbols>true</DebugSymbols>
     <DebugType>portable</DebugType>
   </PropertyGroup>
   ```

## 📊 Автоматизация профилирования

### CI/CD интеграция:

```yaml
# .github/workflows/performance.yml
- name: Run Performance Tests
  run: |
    dotnet tool install -g JetBrains.dotTrace.CommandLineTools
    dotTrace -- dotnet test --filter "Category=Performance"
    dotTrace report -o=perf-report.html snapshot.dtp
```

## 🎓 Дополнительные ресурсы

- [dotTrace Documentation](https://www.jetbrains.com/help/profiler/)
- [.NET MAUI Performance](https://docs.microsoft.com/en-us/dotnet/maui/fundamentals/performance)
- [Rider Performance Profiling](https://www.jetbrains.com/help/rider/Performance_Profiling.html) 