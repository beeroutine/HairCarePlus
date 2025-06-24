# Clinic Chat Module (MAUI)

Version: 2025-06-22

## Overview
Real-time chat between clinic staff (doctor side) and a single patient. Built on ASP.NET Core SignalR + Clean Architecture.

## Layers
```
UI (XAML / MVVM)
   ↳ Application (CQRS commands/queries ‑ planned)
      ↳ Infrastructure (SignalR client, navigation)
         ↳ Shared DTOs (HairCarePlus.Shared.Communication)
```

## Key Classes
| Path | Responsibility |
|------|----------------|
| `ChatPage.xaml` | UI layout: bubble list, reply preview, input panel |
| `ChatViewModel.cs` | State: `Messages`, `ReplyToMessage`; commands for send / reply / cancel |
| `SignalRChatHubConnection.cs` | Wraps `HubConnection`, exposes `MessageReceived` & `SendMessageAsync` |
| `IChatHubConnection` | Interface for DI / unit-testing |

## User flow
1. ViewModel calls `InitializeAsync()` → ensures hub connection, joins group `default_conversation`.
2. CollectionView displays messages with triggers:
   * `SenderId == doctor` → blue bubble right.
   * Else (patient) → green bubble left.
3. Tap a patient bubble → `HandleReplyToMessage` → sets `ReplyToMessage` and shows green preview bar.
4. Press ↑ Send → `SendAsync` passes `replyToSenderId/replyToContent` to the hub.
5. Server broadcasts `ChatMessageDto` containing reply meta.
6. Both clients add message; XAML shows embedded reply preview inside the bubble.

## Extension points
| Feature | How to add |
|---------|-----------|
| Message editing | Implement `EditMessageCommand`, extend DTO with `MessageId` and update API |
| Multi-conversation | Pass dynamic `conversationId` (patientId) when navigating |
| Media | Support `MessageType.Image` like in Patient app (UI template already in XAML) |

## Accessibility & Theming
* Semantic colors via `AppThemeBinding`.
* SwipeView actions have transparent backgrounds (uses system highlight).
* All interactive elements ≥ 44 pt.

## Testing
```
dotnet test src/Client/HairCarePlus.Client.Clinic/Features/Chat/Tests
```
(unit tests to be added – skeleton project in place)

## UI / UX
| Zone | Details |
|------|---------|
| **Header** | back button, **patient name**, presence indicator (restored by using `Shell.TitleView`; no `NavigationPage.HasNavigationBar`) |
| **Message list** | `CollectionView` + `AutoScrollToBottomBehavior` keeps newest message visible; colour swap (blue = doctor, green = patient); `SwipeView` for edit/delete |
| **Input panel** | attachment (+), `Editor`, send (↑); reply bar shows when `ReplyToMessage` set |

---
Last updated: 2025-06-22 