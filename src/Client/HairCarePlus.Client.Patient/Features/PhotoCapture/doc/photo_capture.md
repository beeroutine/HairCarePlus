# Photo Capture Module

> Version: September 2025 | .NET MAUI 9.0.51 SR

## TL;DR
Guides patients to capture transplant-area photos with AR overlays, caches images locally, and routes them to chat and progress modules.

## Table of Contents
1. [Overview](#overview)
2. [Features](#features)
3. [Architecture](#architecture)
4. [Data Flow](#data-flow)
5. [UI/UX](#uiux)
6. [Technical Implementation](#technical-implementation)
7. [Platform Considerations](#platform-considerations)
8. [Future Work](#future-work)

## Overview
The Photo Capture module allows patients to take aligned photos of transplant areas using AR overlays. Captured images are cached locally and integrated into chat and progress tracking.

## Features
- AR overlay templates for frontal, temporal, and occipital zones
- Photo capture via `CameraView.CaptureImage()`
- Local caching to `[CacheDirectory]/HairCarePlus/captured_photos/`
- Automatic `PhotoCapturedMessage` publication for ChatViewModel handling
- Cross-platform fallback using CommunityToolkit.Maui.Camera
- Clean Architecture with CQRS and MVVM separation

## Architecture
```
PhotoCapture/
├── Application/
│   ├── Commands/CapturePhotoCommand.cs
│   ├── Queries/GetCaptureTemplatesQuery.cs
│   └── Messages/PhotoCapturedMessage.cs
├── Domain/Entities/CaptureTemplate.cs
├── Services/
│   ├── Interfaces/ICameraArService.cs
│   └── Implementation/CameraArService.cs
├── ViewModels/PhotoCaptureViewModel.cs
├── Views/PhotoCapturePage.xaml (+ .cs)
└── Styles/PhotoCaptureStyles.xaml
```

## Data Flow
1. Navigate to `//camera` opens `PhotoCapturePage`
2. `OnAppearing` starts `CameraView` preview
3. ViewModel sends `GetCaptureTemplatesQuery`
4. On shutter tap, `CameraView.CaptureImage()` triggers `MediaCaptured`
5. Page saves image via `IMediaFileSystemService.SaveFileAsync()` and publishes `PhotoCapturedMessage`
6. ChatViewModel handles the message and displays the photo

## UI/UX
- `CameraView` preview with transparent AR overlay PNGs
- Horizontal `CollectionView` template selector
- Shutter button centered at bottom
- Switch-camera button at top-right
- **Navigation:** страница отображается полноэкранно (`Shell.TabBarIsVisible="False"`), нижняя TabBar скрыта; возврат к прошлой странице осуществляется кнопкой «←» в `Shell.TitleView`.

## Technical Implementation
- **Models:** `CaptureTemplate`, `PhotoCapturedMessage`
- **ViewModel:** `PhotoCaptureViewModel` with observable properties and CQRS commands
- **Commands/Queries:** `CapturePhotoCommand`, `GetCaptureTemplatesQuery`
- **DI Registration:**
  ```csharp
  services.AddScoped<ICameraArService, CameraArService>();
  services.AddTransient<PhotoCaptureViewModel>();
  services.AddTransient<PhotoCapturePage>();
  services.AddScoped<ICommandHandler<CapturePhotoCommand>, CapturePhotoHandler>();
  services.AddScoped<IQueryHandler<GetCaptureTemplatesQuery, IReadOnlyList<CaptureTemplate>>, GetTemplatesHandler>();
  ```

## Platform Considerations
- ARKit/ARCore native integration pending
- Ensure camera permissions on iOS/Android
- Handle `MediaCaptured` and lifecycle events correctly across platforms

## Future Work
- Implement native AR alignment and tracking
- Enable reliable front/back camera switching via handler or platform code
- Optimize image caching, memory usage, and sync strategies

---
© HairCare+, 2025 🩺 