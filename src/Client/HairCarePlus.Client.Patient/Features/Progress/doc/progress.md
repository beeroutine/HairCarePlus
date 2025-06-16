# Модуль Progress — Техническая документация

> Версия: 3.0 | Последнее обновление: январь 2025

## Содержание
1. [Обзор архитектуры](#обзор-архитектуры)
2. [Компоненты модуля](#компоненты-модуля)
3. [Система ограничений и визуализация](#система-ограничений-и-визуализация)
4. [Лента прогресса](#лента-прогресса)
5. [UI/UX принципы](#uiux-принципы)
6. [Техническая реализация](#техническая-реализация)

## Обзор архитектуры

Модуль Progress следует Clean Architecture с четким разделением слоев:

```
Features/Progress/
├── Application/         # CQRS команды и запросы
├── Domain/             # Доменные сущности и логика
├── Services/           # Сервисы и адаптеры
├── ViewModels/         # MVVM ViewModels
├── Views/              # XAML views и code-behind
├── Converters/         # Value converters
├── Styles/             # XAML стили и ресурсы
└── doc/                # Документация модуля
```

## Система ограничений и визуализация

### Prohibition Sign дизайн
Модуль использует качественные prohibition signs (🚫) для отображения ограничений:

**Компоненты prohibition sign:**
- **Базовый круг** - Border с Ellipse shape (64x64px)
- **SVG иконка** - центрированная иконка (28x28px) 
- **Диагональная линия** - Path элемент с закругленными концами
- **Badge с днями** - правый верхний угол (22x22px)

**Цветовая кодировка:**
- **Критичные** (1 день): красный (#DC4545), толщина 4-4.5px
- **Скоро истекающие** (2-3 дня): оранжевый (#F59E0B), толщина 3.5-4px
- **Завершенные** (0 дней): зеленый (#10B981), opacity 0.7
- **Обычные**: серый, толщина 3px

### Система иконок

**SVG иконки** (основной способ):
```csharp
// Путь: Resources/AppIcon/svg/{название}.svg
RestrictionIconType.NoSmoking => "Resources/AppIcon/svg/no_smoking.svg"
```

**FontAwesome fallback** (резервный способ):
```csharp
// Доступно через GetFontAwesomeIcon()
RestrictionIconType.NoSmoking => "\uf54d" // fa-smoking-ban
```

**Доступные типы ограничений:**
- `NoSmoking` - запрет курения  
- `NoAlcohol` - запрет алкоголя
- `NoSex` - запрет половой активности
- `NoHairCutting` - запрет стрижки
- `NoHatWearing` - запрет ношения шляп
- `NoStyling` - запрет укладки
- `NoLaying` - запрет лежания
- `NoSun` - запрет солнца
- `NoSweating` - запрет потения
- `NoSwimming` - запрет плавания
- `NoSporting` - запрет спорта
- `NoTilting` - запрет наклонов головы

## Лента прогресса

### Instagram-подобный дизайн
Лента прогресса следует современным принципам:

**Карточка прогресса:**
- Минималистичный border radius 16px
- Тонкие тени для глубины
- Day badge (левый верхний угол)
- AI Score badge (правый верхний угол)
- Instagram-подобные индикаторы фото
- Комментарии врача/AI с цветовой кодировкой

**Структура карточки:**
```
┌─ Day 10 ────────────────── AI 56 ─┐
│  ┌─────────────────────────────┐   │
│  │        Фото пациента        │   │
│  └─────────────────────────────┘   │
│  ●●○ (индикаторы фото)            │
│  23 May 2025                       │
│  ┌─ ● Комментарий врача ──────┐   │
│  │ Прогресс идет отлично...   │   │
│  └───────────────────────────────┘   │
└───────────────────────────────────┘
```

## UI/UX принципы

### Минималистичный дизайн
- **Без заголовка** - убран title "Progress" для чистоты
- **Цветовая схема** - черный/белый/серый вместо purple
- **Спейсинг** - 24px сверху, 20px по бокам, 16px между элементами
- **Соответствие Today Page** - единый стиль с главной страницей

### Визуальная иерархия  
- **Restriction timers** - горизонтальный скролл сверху
- **Лента прогресса** - основной контент с pull-to-refresh
- **Empty state** - дружелюбная заглушка с эмодзи и инструкциями

### Accessibility
- `AutomationProperties` для всех интерактивных элементов
- Семантические цвета для состояний
- Достаточный контраст для читабельности
- Поддержка screen readers

### Навигация
`ProgressPage` отображается внутри стандартной нижней панели вкладок Shell. Свойство `Shell.TabBarIsVisible` **не** изменяется, поэтому TabBar остаётся видимым; верхний нативный NavigationBar скрыт (`Shell.NavBarIsVisible="False"`).

## Техническая реализация

### MVVM и Data Binding
```xml
<!-- Compiled bindings обязательны -->
<DataTemplate x:DataType="models:RestrictionTimer">
    <Grid>
        <!-- Path вместо Line для качественной графики -->
        <Path Data="M 14,14 L 50,50" 
              StrokeLineCap="Round" 
              StrokeThickness="3.5" />
    </Grid>
</DataTemplate>
```

### Converters
- `RestrictionIconConverter` - SVG иконки с FontAwesome fallback
- `RestrictionShortPhraseConverter` - короткие фразы для UI
- `EqualToConverter` - сравнения для triggers

### Visual State Management
```xml
<VisualStateManager.VisualStateGroups>
    <VisualStateGroup x:Name="UrgencyStates">
        <VisualState x:Name="Critical">
            <!-- Пульсация для критичных ограничений -->
        </VisualState>
    </VisualStateGroup>
</VisualStateManager.VisualStateGroups>
```

### Performance оптимизации
- **Virtualization** в CollectionView
- **Image caching** для фото
- **Lazy loading** для тяжелых элементов
- **Background loading** для данных

---

**Примечания:**
- Все стили наследуют от базовых app themes
- Path элементы предпочтительнее Line для кросс-платформенности  
- SVG иконки имеют FontAwesome fallback для надежности
- Prohibition sign дизайн соответствует международным стандартам UI
