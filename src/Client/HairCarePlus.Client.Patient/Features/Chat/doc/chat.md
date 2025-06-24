# Chat Module (Patient App)

> Updated: June 2025 | Tested on .NET MAUI 9.0.51 SR

---

## Overview
Real-time one-to-one messaging between the patient and the clinic. Implementation focuses on text chat with optimistic UI and offline cache; media attachments are partially supported (camera → image message).

## Architecture (Clean Architecture + MVVM + CQRS)
```
Chat/
├── Application/
│   ├── Commands/   # SendChatMessageCommand, UpdateChatMessageCommand …
│   ├── Queries/    # GetChatMessagesQuery
│   └── Messages/   # (domain events)
├── Domain/
│   └── Entities/   # ChatMessage, Doctor
├── Infrastructure/
│   └── Repositories/  ChatMessageRepository (EF Core / cache)
├── Services/         # Sync, Notification, etc.
├── ViewModels/       # ChatViewModel.cs
└── Views/            # ChatPage.xaml (+ .cs)
```
Dependencies: UI → **Application** → Domain → Infrastructure

## Current Functionality
| Feature | Status | Notes |
|---------|--------|-------|
| Text messages | ✅ | Patient → doctor, optimistic insertion in UI |
| Reply to message | ✅ | Tap on a bubble to quote doctor's message (cannot reply to own) |
| Edit / Delete own message | ✅ | Swipe right to reveal **Edit / Delete** actions (only for patient-sent) |
| Image from camera | ✅ | `OpenCameraCommand` navigates to camera feature; result sent as `MessageType.Image` |
| Image picker | ⏳ | Menu item exists but shows *Coming Soon* alert |
| Delivery status (sent/delivered/read) | 🟡 | Enum present; doctor response is simulated and marks `Delivered` |
| Presence indicator | ✅ | `Doctor.IsOnline` toggles header text 'Online/Offline' (mock) |
| Threading / search / E2E encryption | ❌ | Not yet implemented |

## UI / UX
| Zone | Details |
|------|---------|
| **Header** | back button, doctor name, presence indicator |
| **Message list** | `CollectionView` + new `AutoScrollToBottomBehavior` → всегда прокручивает к последнему сообщению (Telegram-style); `SwipeView` for edit/delete; reply preview via coloured frame |
| **Input panel** | attachment (+) button, `Editor`, send (↑) button; reply or edit bar shows above input |
| **Theming** | Colours via `ResourceDictionary`, bubbles change for patient / doctor & light/dark |
| **Typography** | Inherited from global style → `OpenSansRegular` & `OpenSansSemibold` |

### Gestures
• **Tap** bubble → start reply (if doctor message)  
• **Swipe right→left** on own message → Edit / Delete  
• **Tap** reply × button → cancel  
• Editor `Send` command hides keyboard and scrolls to newest.

### Navigation pattern
`ChatPage` is presented full-screen. It sets `Shell.TabBarIsVisible="False"` and shows a custom Back button in `Shell.TitleView`. This differentiates it from Today/Progress pages where the bottom `TabBar` remains visible.

## Data Flow
1. **OnAppearing** `