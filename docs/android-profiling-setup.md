# 🤖 Настройка профилирования Android в Rider

## Создание правильной конфигурации

### Вариант 1: Исправление текущей конфигурации

1. В поле **Launch profile** выберите: `Android`
2. Убедитесь что:
   - **Target framework**: `net9.0-android`
   - **Runtime**: `<Automatic>` или `Mono`

### Вариант 2: Создание новой конфигурации

1. Нажмите `+` → `.NET Launch Settings Profile`
2. Настройте:
   ```
   Name: HairCarePlus Android Profile
   Project: HairCarePlus.Client.Patient
   Launch Profile: Android
   ```

### Настройка для профилирования

#### 1. Подготовка устройства/эмулятора

```bash
# Для эмулятора
adb shell setprop debug.mono.profile '127.0.0.1:9999'

# Включите режим разработчика на устройстве
# Settings → About → Tap Build Number 7 times
# Settings → Developer Options → USB Debugging: ON
```

#### 2. Оптимальные параметры для профилирования

В **Environment variables** добавьте:
```
MONO_PROFILE_OPTIONS=log:calls,alloc,output=profile.mlpd
ANDROID_ENABLE_PROFILING=true
DOTNET_CLI_TELEMETRY_OPTOUT=1
```

#### 3. Запуск профилирования

1. Выберите вашу конфигурацию
2. Нажмите на иконку профилирования (◐)
3. В диалоге выберите:
   ```
   Profiling type: Timeline
   ✅ Collect Android traces
   ✅ Collect managed allocations
   ✅ Collect native allocations
   ```

### Типичные проблемы Android

#### Ошибка: "No devices found"
```bash
# Проверьте подключение
adb devices

# Перезапустите ADB
adb kill-server
adb start-server
```

#### Ошибка: "Failed to start profiling"
1. Убедитесь что приложение в Debug mode
2. Проверьте AndroidManifest.xml:
   ```xml
   <application android:debuggable="true" ...>
   ```

### Специфика профилирования TodayPage на Android

#### Что проверять:

1. **RecyclerView производительность**
   - Время создания ViewHolder
   - Эффективность переиспользования

2. **Память**
   - Java heap использование
   - Native память (особенно для изображений)

3. **Rendering**
   - GPU overdraw
   - Jank frames при скролле

#### Метрики для Android:

| Компонент | Целевое время | Критическое |
|-----------|--------------|-------------|
| LoadTodayEventsAsync | < 150ms | > 300ms |
| RecyclerView scroll | 60 FPS | < 30 FPS |
| Memory per item | < 1MB | > 2MB |
| Startup time | < 2s | > 4s |

### Анализ результатов

После профилирования проверьте:

1. **Timeline → UI Thread**
   - Не должно быть блокировок > 16ms
   - Ищите красные участки

2. **Memory Traffic**
   - Частые GC = проблема
   - Растущая память = утечка

3. **Call Tree**
   - Фокус на ваш код, не системный
   - Ищите TodayViewModel методы 