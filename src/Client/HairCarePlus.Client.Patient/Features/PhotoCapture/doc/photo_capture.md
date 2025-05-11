# PhotoCapture — съёмка с AR-шаблонами

> Версия спецификации: май 2025 (дополнено)  
> Относится к приложению: **HairCarePlus.Client.Patient**  
> Статус реализации: MVP (захват фото работает, AR и переключение камер — WIP)

## Назначение
Функция позволяет пациенту делать фотографию зоны трансплантации с помощью AR-оверлея, который упрощает позиционирование камеры и контроль освещённости. Полученное изображение автоматически прикрепляется к чату с клиникой, а в дальнейшем — к прогресс-метрикам.

## Ключевые возможности
* **AR-шаблоны зон**: фронтальная, теменная, затылочная (выбор работает, overlays отображаются).
* **Захват фото**: Кнопка затвора инициирует захват фото через `CameraView.CaptureImage()`.
* **Сохранение фото**: Фото сохраняется в кэш приложения (`[CacheDirectory]/HairCarePlus/captured_photos/`).
* **Автоматическое прикрепление в чат**: после съёмки и сохранения, `PhotoCapturedMessage` отправляется, что позволяет `ChatViewModel` обработать и отобразить фото.
* **Кроссплатформенный fallback**: Пока используется `CameraView` из Community Toolkit. Нативная AR-реализация (ARKit/ARCore) не начата.
* **Чистая архитектура**: CQRS, MVVM, DI, разделение слоёв UI / Application / Domain / Infrastructure.

## Стек технологий (актуальный)
*   .NET 9 SDK (9.0.200)
*   .NET MAUI (9.0.51 SR)
*   CommunityToolkit.Maui (9.0.0)
*   CommunityToolkit.Maui.Camera (2.0.3)
*   ASP.NET Core Web API + SignalR (для серверной части)
*   Entity Framework Core (для серверной части и локальной БД клиента)

## Структура проекта
```text
PhotoCapture/
├── Application/
│   ├── Commands/
│   │   └── CapturePhotoCommand.cs
│   ├── Queries/
│   │   └── GetCaptureTemplatesQuery.cs
│   └── Messages/
│       └── PhotoCapturedMessage.cs
├── Domain/
│   └── Entities/
│       └── CaptureTemplate.cs
├── Services/  // ICameraArService и его базовая реализация пока не используются активно CameraView
│   ├── Interfaces/ICameraArService.cs
│   └── Implementation/
│       └── CameraArService.cs
├── Styles/
│   └── PhotoCaptureStyles.xaml
├── ViewModels/PhotoCaptureViewModel.cs
├── Views/
│   ├── PhotoCapturePage.xaml(+ .cs)
│   └── Converters/ (*.cs) 
└── doc/photo_capture.md (этот файл)
```

## Поток данных (захват фото)
1.  Пользователь нажимает кнопку камеры в TabBar.  
    `Shell.GoToAsync("//camera")` открывает `PhotoCapturePage`.
2.  `PhotoCapturePage.OnAppearing` запускает `CameraView.StartCameraPreview()`.
3.  `PhotoCaptureViewModel` загружает шаблоны (`GetCaptureTemplatesQuery`). Пользователь выбирает шаблон.
4.  Пользователь нажимает кнопку-затвор.
    *   `PhotoCapturePage.OnShutterTapped` вызывает `CameraView.CaptureImage()`.
5.  `CameraView` генерирует событие `MediaCaptured`.
6.  `PhotoCapturePage.OnMediaCaptured` обрабатывает событие:
    *   Сохраняет данные изображения (stream) в файл через `IMediaFileSystemService.SaveFileAsync()` в директорию `[CacheDirectory]/HairCarePlus/captured_photos/`.
    *   Публикует `PhotoCapturedMessage` с путем к файлу.
7.  `ChatViewModel` (подписчик) получает сообщение, формирует UI для нового сообщения с картинкой и отправляет `SendChatImageCommand`.

## UI (MVP)
*   **Camera preview**: `CommunityToolkit.Maui.Camera.CameraView`.
*   **Overlay Image**: Прозрачный PNG поверх превью, обновляется при выборе шаблона.
*   **Template picker**: Горизонтальный `CollectionView` сверху с стилизованными "чипами".
*   **Capture button**: Круглая кнопка-затвор внизу по центру.
*   **Switch camera button**: Иконка-кнопка сверху справа (логика переключения не работает).

## AR-шаблоны
| Id    | Название         | Файл overlay     | Дистанция, мм | Lux |
|-------|------------------|------------------|---------------|-----|
| front | Фронтальная зона | front_head.png   | 300           | 300+ |
| top   | Темя             | top_head.png     | 350           | 400+ |
| back  | Затылок          | back_head.png    | 300           | 300+ |

Шаблоны хранятся в `Resources/Images/`. **BuildAction**: `MauiImage`.

## Зависимости DI (PhotoCaptureServiceExtensions)
```csharp
services.AddScoped<ICameraArService, CameraArService>(); // Пока не используется CameraView напрямую
services.AddTransient<PhotoCaptureViewModel>();
services.AddTransient<PhotoCapturePage>();
services.AddScoped<ICommandHandler<CapturePhotoCommand>, CapturePhotoHandler>(); // Команда для старого подхода с ICameraArService
services.AddScoped<IQueryHandler<GetCaptureTemplatesQuery, IReadOnlyList<CaptureTemplate>>, GetCaptureTemplatesHandler>();
```
*Примечание: `CapturePhotoCommand` и `ICameraArService` были частью первоначального дизайна. Текущая реализация захвата фото идет через `CameraView` напрямую из code-behind страницы.*

## Текущие проблемы и направления для исследования (Deep Research Section)

1.  **Переключение камеры (Front/Back) - КРИТИЧЕСКИ НЕ РАБОТАЕТ**:
    *   **Симптомы**: Команда `ToggleFacingCommand` в `PhotoCaptureViewModel` корректно меняет внутреннее свойство `Facing` (Front/Back). Однако, код в `PhotoCapturePage.xaml.cs` (`ApplyFacing`, который должен был реагировать на изменение этого свойства и физически переключать камеру) был закомментирован, так как приводил к многочисленным ошибкам компиляции (CS0117, CS1061, CS0234).
    *   **Проблема**: API `CameraView` из `CommunityToolkit.Maui.Camera` **версии 2.0.3**, похоже, не предоставляет очевидных свойств типа `CameraView.Cameras` (как коллекция `CameraInfo`) или `CameraView.Camera` (для установки выбранной `CameraInfo`), а также перечисления `CameraPosition` в том виде, как это могло быть в других версиях или как ожидалось. Попытки использовать `CommunityToolkit.Maui.Core.Primitives.CameraPosition` и `CommunityToolkit.Maui.Core.Primitives.CameraInfo` также не увенчались успехом с данной конфигурацией пакетов.
    *   **Нужно исследовать**:
        *   Какой официальный способ переключения между передней и задней камерой для `CameraView` в `CommunityToolkit.Maui.Camera` **v2.0.3**? Существует ли команда на самом элементе `CameraView` (например, `SwitchCameraCommand`) или специальное свойство/метод, которое не было найдено в документации?
        *   Совместимость версий: Убедиться, что используемые версии `CommunityToolkit.Maui` (9.0.0) и `CommunityToolkit.Maui.Camera` (2.0.3) полностью совместимы в контексте API камеры.
        *   Альтернативные подходы: Если прямого API нет, возможно ли это сделать через платформенный код или хендлеры?

2.  **Ошибка загрузки шрифта `MaterialSymbolsOutlined.ttf`**:
    *   **Симптомы**: В логах появляются ошибки `System.IO.FileNotFoundException: Native font with the name MaterialSymbolsOutlined.ttf was not found` от `Microsoft.Maui.FontRegistrar`.
    *   **Проблема**: Шрифт либо не добавлен корректно в проект (например, отсутствует в `.csproj` как `MauiFont` или неправильный Build Action), либо есть проблема с его регистрацией/именем.
    *   **Нужно исследовать**: Проверить регистрацию шрифта в `MauiProgram.cs` и его наличие/настройки в файле проекта.

3.  **Ошибка `TaskCanceledException` при `StartCameraPreview` (спорадически?)**:
    *   **Симптомы**: В логах была замечена `System.Threading.Tasks.TaskCanceledException: A task was canceled.` при вызове `Camera.StartCameraPreview(CancellationToken.None);` в `OnAppearing`.
    *   **Проблема**: Может быть связана с жизненным циклом страницы/камеры, слишком быстрым вызовом до полной инициализации, или конфликтом с другими операциями.
    *   **Нужно исследовать**: Насколько стабильно воспроизводится? Есть ли условия, при которых это происходит чаще? Возможно, требуется проверка состояния камеры перед вызовом или использование `try-catch` с более специфичной обработкой.

4.  **Реализация AR-оверлеев**: Текущий overlay - просто `Image`. Нативная AR-реализация не начата.

---
© HairCare+, 2025 🩺 