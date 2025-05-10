# HairCare+ — Core Development Guidelines

> **Version:** 2.0   |  **Last update:** Sept 2025 
>
> This document captures ONLY the minimum yet critical information every contributor must know before touching the codebase.  
> For in-depth details see the feature-level docs under `src/**/doc/`.

---

## 1  Project Overview
### 1.1  Mission – *Reduce Post-Surgery Anxiety*
HairCare+ exists to help hair-transplant patients feel **confident and in control** during their recovery.  
We achieve this via:  
• Daily gamified calendar tasks that drive adherence to the care plan.  
• Real-time chat & notifications connecting patient and clinic.  
• Rich media (photo/video) progress tracking.  
The same platform lets clinics monitor outcomes and adjust treatment.

HairCare+ is a cross-platform ecosystem that consists of:
1. **Patient App** (MAUI) – self-care calendar, media, chat.  
2. **Clinic App** (MAUI) – patient monitoring & analytics.  
3. **Server** (ASP.NET Core) – auth, sync, real-time SignalR hub.

## 2  Folder Layout (high level)
```
src/
  Client/
    HairCarePlus.Client.Patient/
    HairCarePlus.Client.Clinic/
  Server/
    HairCarePlus.Server.*
  Shared/
    HairCarePlus.Shared.*
```

### 2.1  Feature Scaffolding Convention
Each client feature lives in its own folder and mirrors Clean-Architecture sub-layers:

```
HairCarePlus.Client.[App]/
  Features/
    <FeatureName>/
      Application/   # CQRS commands/queries/handlers
      Domain/        # (optional) feature-level domain objects
      Services/      # Interfaces + implementations (local cache, API, etc.)
      ViewModels/
      Views/
      doc/           # feature docs & UX specs
```

Infrastructure-wide concerns (e.g., Connectivity, Storage) reside in `Client/Infrastructure` and are consumed via DI.

`Client/Common` holds reusable UI bits (Behaviors, Converters, base pages) that are referenced by multiple features but are not global enough for Shared/.

## 3  Tech Stack
* .NET 9 SDK (SDK 9.0.200)
* .NET MAUI 9.0.51 SR
* ASP.NET Core Web API + SignalR  
* Entity Framework Core  
* Clean Architecture · MVVM · CQRS · SOLID · KISS

## 4  Architecture Rules (TL;DR)
* 4-layer Clean Architecture: **UI → Application → Domain → Shared Kernel**.  
* CQRS: commands **mutate**, queries **read**; handled via in-memory buses on client, MediatR on server.  
* No layer may reference a more external layer.  
* Domain contains NO persistence/UI code.

## 5  MAUI UI Checklist
* Use **Shell + CollectionView + Border + VisualStateManager**; avoid deprecated controls (`ListView`, `Frame`, `SwipeGestureRecognizer`).  
* Enable **compiled bindings** (`x:DataType`). Converters only when unavoidable.  
* Theme via **ResourceDictionary + AppThemeBinding** – no hard-coded hex in XAML.  
* Interactions: `SwipeView`, `CommunityToolkit.TouchBehavior` (long-press).  
* Log with `ILogger<T>`; never `Debug.WriteLine`.

## 6  Cross-cutting Policies
* **Security:** JWT, HTTPS everywhere, Secure Storage on device; no secrets in code.  
* **Testing:** `dotnet test` must pass locally & in CI; unit-test domain & application layers.  
* **CI/CD:** build + lint + test on every PR; fail on warnings as errors.  
* **Documentation:** keep feature docs (`doc/*.md`) in sync with code changes.

## 7  Key References
* README (this file) – core rules.  
* Calendar Today page – `src/Client/HairCarePlus.Client.Patient/Features/Calendar/doc/todaypage.md`.  
* Calendar module overview – `.../Calendar/doc/overview.md`.  
* Chat module – `.../Chat/doc/chat.md`.

---
👍  Follow the rules above, keep the codebase clean, and happy shipping!