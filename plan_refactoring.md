# План рефакторинга HairCarePlus Patient Mobile Application

## Цель
Реорганизовать существующий проект в соответствии с целевой архитектурой, описанной в HairCarePlus_Patient.md, следуя принципам Clean Architecture, MVVM и модульной организации кода.

## 1. Реорганизация структуры проекта

### 1.1 Целевая структура
```
HairCarePlus.Client.Patient/
├── Features/                    # Функциональные модули
│   ├── Calendar/               # Календарь процедур и событий
│   │   ├── Domain/            # Доменный слой календаря
│   │   │   ├── Entities/     # Доменные сущности
│   │   │   └── Repositories/ # Интерфейсы репозиториев
│   │   ├── Services/         # Доменные сервисы
│   │   ├── ViewModels/       # Логика представлений
│   │   └── Views/           # UI компоненты
│   ├── Chat/                  # Модуль чата
│   │   ├── Domain/
│   │   ├── Services/
│   │   ├── ViewModels/
│   │   └── Views/
│   └── Notifications/         # Модуль уведомлений
├── Infrastructure/            # Инфраструктурный слой
│   ├── Storage/              # Базовая инфраструктура хранения
│   ├── Security/
│   └── Media/
├── Common/                    # Общие компоненты и утилиты
├── ViewModels/                # Базовые ViewModel'и
├── Effects/                   # Эффекты MAUI
├── Resources/                 # Ресурсы приложения
└── Platforms/                 # Платформо-зависимый код
```

### 1.2 Шаги реорганизации
1. Создать основные директории структуры
2. Перенести существующие файлы в соответствующие директории
3. Обновить пространства имен (namespaces)
4. Удалить неиспользуемые файлы

## 2. Рефакторинг по модулям

### 2.1 Calendar Feature
1. **Доменный слой**
   - Создать HairTransplantEvent.cs
   - Определить ICalendarRepository.cs
   - Реализовать доменные сервисы
   - Добавить ICalendarDataInitializer.cs для первичной генерации
   - Добавить ICalendarSyncService.cs для синхронизации
   - **Генерация событий**: используется JsonHairTransplantEventGenerator, который читает HairTransplantSchedule.json (Build Action: Content, Copy if newer). day=1 в json соответствует дню операции (DateTime.Today при инициализации), day=2 — следующий день и т.д. Диапазон генерации — 1 год. Генератор регистрируется в DI как Singleton:
   ```csharp
   services.AddSingleton<IHairTransplantEventGenerator, JsonHairTransplantEventGenerator>();
   ```

2. **Инфраструктурный слой**
   - Реализовать CalendarRepository
   - Настроить локальное хранение
   - Добавить CalendarDataInitializer:
     * Проверка первого запуска
     * Генерация базового набора событий
     * Сохранение в локальное хранилище
   - Добавить CalendarSyncService:
     * Проверка актуальности данных
     * Инкрементальное обновление
     * Обработка изменений в событиях

3. **Логика работы с данными**
   ```csharp
   public class CalendarService
   {
       private readonly ICalendarRepository _repository;
       private readonly ICalendarDataInitializer _initializer;
       private readonly ICalendarSyncService _syncService;

       public async Task<IEnumerable<CalendarEvent>> GetEventsForDateAsync(DateTime date)
       {
           // Проверка и инициализация при первом запуске
           if (await _initializer.NeedsInitialization())
           {
               await _initializer.InitializeData();
           }

           // Получение данных из локального хранилища
           var events = await _repository.GetEventsForDateAsync(date);

           // Проверка необходимости синхронизации
           if (await _syncService.NeedsSynchronization())
           {
               await _syncService.SynchronizeEvents();
               events = await _repository.GetEventsForDateAsync(date);
           }

           return events;
       }
   }
   ```

4. **Стратегия хранения данных**
   - SQLite для основного хранения событий
   - Preferences для хранения метаданных (дата последней синхронизации, версия данных)
   - Кэширование часто используемых данных в памяти

5. **Обработка изменений**
   - Отслеживание изменений в событиях (completed, modified)
   - Сохранение изменений локально
   - Синхронизация изменений с сервером при наличии подключения

### 2.2 Chat Feature
1. **Доменный слой**
   - Создать ChatMessage.cs
   - Определить IChatRepository.cs
   - Реализовать доменные сервисы

2. **Инфраструктурный слой**
   - Реализовать ChatRepository
   - Настроить SignalR
   - Добавить локальное хранение истории

3. **Презентационный слой**
   - Создать ChatViewModel
   - Реализовать ChatView
   - Добавить real-time обновления

### 2.3 Notifications Feature
1. **Доменный слой**
   - Создать Notification.cs
   - Определить INotificationRepository.cs

2. **Инфраструктурный слой**
   - Реализовать NotificationService
   - Настроить push-уведомления
   - Добавить локальные уведомления

## 3. Infrastructure Layer

### 3.1 Storage
1. Настроить EF Core с SQLite
2. Создать базовые репозитории
3. Реализовать миграции
4. Реализовать механизм версионирования данных
5. Добавить систему синхронизации
6. Настроить кэширование

### 3.2 Security
```csharp
Infrastructure/Security/
└── SecureStorageService.cs
```

1. Реализовать JWT аутентификацию
2. Настроить SecureStorage
3. Добавить шифрование данных

### 3.3 Media
```csharp
Infrastructure/Media/
└── FileSystemService.cs
```

1. Реализовать работу с файлами
2. Добавить кэширование
3. Настроить очистку временных файлов

## 4. Dependency Injection

### 4.1 Базовая конфигурация
```csharp
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    
    // Core registrations
    builder.Services.AddDbContext<AppDbContext>();
    
    // Feature registrations
    builder.Services
        .AddCalendarFeature()
        .AddChatFeature()
        .AddNotificationsFeature();
        
    // Infrastructure
    builder.Services
        .AddSingleton<ILocalStorageService, LocalStorageService>()
        .AddSingleton<ISecureStorageService, SecureStorageService>();
        
    return builder.Build();
}
```

### 4.2 Feature Extensions
```csharp
public static class CalendarFeatureExtensions
{
    public static IServiceCollection AddCalendarFeature(this IServiceCollection services)
    {
        services.AddScoped<ICalendarRepository, CalendarRepository>();
        services.AddScoped<ICalendarService, CalendarService>();
        services.AddTransient<CalendarViewModel>();
        return services;
    }
}
```

## 5. UI/UX Обновления

### 5.1 Цветовая схема
```xaml
<ResourceDictionary>
    <Color x:Key="MedicationColor">#4B9BF8</Color>
    <Color x:Key="MedicalVisitColor">#A0DAB2</Color>
    <Color x:Key="PhotoColor">#9747FF</Color>
    <Color x:Key="VideoColor">#FF9F1C</Color>
    <Color x:Key="RecommendationColor">#8E8E93</Color>
    <Color x:Key="WarningColor">#F14336</Color>
</ResourceDictionary>
```

### 5.2 Общие стили
1. Создать базовые стили
2. Определить шаблоны
3. Настроить темы

## 6. План выполнения

### Этап 1: Подготовка (1-2 дня)
- [ ] Создать новую структуру папок
- [ ] Настроить базовые проекты
- [ ] Создать основные интерфейсы

### Этап 2: Инфраструктура (2-3 дня)
- [ ] Настроить базу данных
- [ ] Реализовать базовые сервисы
- [ ] Настроить DI

### Этап 3: Функциональные модули (5-7 дней)
- [ ] Перенести и обновить Calendar
- [ ] Реализовать Chat
- [ ] Добавить Notifications

### Этап 4: UI/UX (3-4 дня)
- [ ] Обновить стили
- [ ] Внедрить новый дизайн
- [ ] Добавить анимации

### Этап 5: Тестирование (2-3 дня)
- [ ] Написать unit-тесты
- [ ] Добавить UI тесты
- [ ] Провести интеграционное тестирование

## 7. Критерии успеха

### 7.1 Архитектурные
- [ ] Код следует Clean Architecture
- [ ] Реализован MVVM паттерн
- [ ] Модули независимы и тестируемы

### 7.2 Функциональные
- [ ] Все функции работают офлайн
- [ ] Данные надежно защищены
- [ ] UI соответствует требованиям

### 7.3 Технические
- [ ] Покрытие тестами > 80%
- [ ] Отсутствие утечек памяти
- [ ] Быстрый холодный старт (<2 сек)

## 8. Риски и митигации

### 8.1 Технические риски
- Сложности с миграцией данных
  * Решение: Создать скрипты миграции и тестировать на копии данных
- Проблемы производительности
  * Решение: Профилирование на каждом этапе

### 8.2 Организационные риски
- Сжатые сроки
  * Решение: Приоритизация функционала, MVP подход
- Сложности интеграции
  * Решение: Ранние интеграционные тесты

## 9. Поддержка

### 9.1 Документация
- Обновить README.md
- Создать документацию по архитектуре
- Добавить комментарии API

### 9.2 Мониторинг
- Добавить логирование
- Настроить отслеживание ошибок
- Добавить метрики производительности ds