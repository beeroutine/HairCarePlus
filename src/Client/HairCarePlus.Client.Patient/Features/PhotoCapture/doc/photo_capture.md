# PhotoCapture — съёмка с AR-шаблонами

> Версия спецификации: май 2025  
> Относится к приложению: **HairCarePlus.Client.Patient**  
> Статус реализации: MVP (fallback-режим), ARKit/ARCore — WIP

## Назначение
Функция позволяет пациенту делать фотографию зоны трансплантации с помощью AR-оверлея, который упрощает позиционирование камеры и контроль освещённости. Полученное изображение автоматически прикрепляется к чату с клиникой, а в дальнейшем — к прогресс-метрикам.

## Ключевые возможности
* **AR-шаблоны зон**: фронтальная, теменная, затылочная.  
  Пользователь выбирает шаблон в горизонтальном списке, соответствующий overlay накладывается поверх живого превью.
* **Автоматическое прикрепление в чат**: после съёмки фото сохраняется локально и отправляется как сообщение `MessageType.Image` в текущий диалог.
* **Кроссплатформенный fallback**: если устройство не поддерживает ARKit/ARCore, используется системный MediaPicker с простым overlay.
* **Чистая архитектура**: CQRS, MVVM, DI, разделение слоёв UI / Application / Domain / Infrastructure.

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
├── Services/
│   ├── Interfaces/ICameraArService.cs
│   └── Implementation/
│       ├── CameraArService.cs          (fallback)
│       ├── CameraArService.iOS.cs      (ARKit) *todo*
│       └── CameraArService.Android.cs  (ARCore) *todo*
├── ViewModels/PhotoCaptureViewModel.cs
├── Views/
│   ├── PhotoCapturePage.xaml(+ .cs)
│   └── Templates/OverlayTemplateView.xaml *todo*
└── doc/photo_capture.md (этот файл)
```

## Поток данных
1. Пользователь нажимает кнопку камеры внизу чата или в TabBar.  
   `Shell.GoToAsync("//camera")` открывает `PhotoCapturePage`.
2. `PhotoCaptureViewModel` загружает шаблоны через `GetCaptureTemplatesQuery` и отображает первый overlay.
3. Команда `CaptureCommand` → `ICommandBus.Send(CapturePhotoCommand)`.
4. `CapturePhotoHandler` вызывает `ICameraArService.CaptureAsync()`, сохраняет файл через `IMediaFileSystemService` и публикует `PhotoCapturedMessage`.
5. `ChatViewModel` получает сообщение, формирует optimistic-UI и отправляет `SendChatImageCommand` — фото появляется в чате.

## UI (первый MVP)
* **Camera preview** — пока `BoxView`-заглушка. Будет заменён на платформенный `CameraViewHandler`.
* **Overlay Image** — прозрачный PNG, `AspectFit`, обновляется при выборе шаблона.
* **Template picker** — горизонтальный `CollectionView` снизу.
* **Capture FAB** — кнопка «Сделать фото».

![wireframe](wireframe_placeholder.png)

## AR-шаблоны
| Id   | Название          | Файл overlay        | Дистанция, мм | Lux |
|------|-------------------|---------------------|---------------|-----|
| front | Фронтальная зона | front_overlay.png | 300 | 300+ |
| top   | Темя             | top_overlay.png   | 350 | 400+ |
| back  | Затылок          | back_overlay.png  | 300 | 300+ |

Шаблоны хранятся в `Resources/Images/PhotoCapture/`. **BuildAction**: `MauiImage`.

## Зависимости DI
Регистрация выполняется в `PhotoCaptureServiceExtensions`:
```csharp
services.AddScoped<ICameraArService, CameraArService>();
services.AddTransient<PhotoCaptureViewModel>();
services.AddTransient<PhotoCapturePage>();
services.AddScoped<ICommandHandler<CapturePhotoCommand>, CapturePhotoHandler>();
services.AddScoped<IQueryHandler<GetCaptureTemplatesQuery, IReadOnlyList<CaptureTemplate>>, GetCaptureTemplatesHandler>();
```

## Будущие улучшения
* Реализовать `CameraArService.iOS` (ARKit, `AVCaptureVideoPreviewLayer` + `ARSession`).
* Реализовать `CameraArService.Android` (ARCore + CameraX).
* FAB-кнопка поверх TabBar с анимацией появления.
* Подсказки освещённости (измерение `AmbientLightSensor`).
* Интеграция с модулем Progress Tracking.

---
© HairCare+, 2025 🩺 