# HairCare+ Solution

## Project Overview
HairCare+ is a comprehensive digital healthcare solution that facilitates communication between clinics and patients during the post-operative period. The solution consists of three main applications:

1. **Patient Mobile Application (MAUI)**
   - Patient profile management
   - Local data storage and synchronization
   - Real-time chat with clinic
   - Calendar and notifications
   - Media management with AR support

2. **Server Application**
   - Centralized communication hub
   - Authentication and security
   - External API integration (AI services)
   - Data synchronization
   - Cloud storage management

3. **Clinic Mobile Application (MAUI)**
   - Clinic staff management
   - Patient monitoring and analytics
   - Treatment management
   - Real-time communication
   - AI-assisted diagnostics

## Solution Structure

```
src/
├── Client/
│   ├── HairCarePlus.Client.Patient/           # Patient MAUI Application
│   │   ├── Features/
│   │   │   ├── Authentication/
│   │   │   ├── Calendar/
│   │   │   ├── Chat/
│   │   │   ├── Media/
│   │   │   └── Notifications/
│   │   ├── Infrastructure/
│   │   └── Services/
│   └── HairCarePlus.Client.Clinic/            # Clinic MAUI Application
│       ├── Features/
│       │   ├── Authentication/
│       │   ├── PatientManagement/
│       │   ├── Analytics/
│       │   ├── Chat/
│       │   └── AIAssistant/
│       ├── Infrastructure/
│       └── Services/
├── Server/
│   ├── HairCarePlus.Server.API/               # API Layer
│   ├── HairCarePlus.Server.Application/       # Application Layer
│   ├── HairCarePlus.Server.Domain/            # Domain Layer
│   └── HairCarePlus.Server.Infrastructure/    # Infrastructure Layer
└── Shared/
    ├── HairCarePlus.Shared.Domain/            # Shared Domain Models
    ├── HairCarePlus.Shared.Communication/     # Communication Contracts
    └── HairCarePlus.Shared.Common/            # Common Utilities
```

## Technology Stack
- .NET 8
- .NET MAUI
- ASP.NET Core Web API
- Entity Framework Core
- SignalR for real-time communication
- Azure Services (or alternative cloud provider)

## Getting Started

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 or later
- .NET MAUI workload
- SQL Server (or alternative database)

### Setup Instructions
1. Clone the repository
2. Open the solution in Visual Studio
3. Restore NuGet packages
4. Configure connection strings
5. Run database migrations
6. Launch the desired application

## Architecture

The solution follows Clean Architecture principles with:
- Domain-Driven Design (DDD)
- SOLID principles
- CQRS pattern
- Event-driven architecture
- Repository pattern
- MVVM pattern (in MAUI applications)

## Security

- JWT authentication
- End-to-end encryption for chat
- Secure storage for sensitive data
- API key protection
- HTTPS enforcement

## Contributing

Please read CONTRIBUTING.md for details on our code of conduct and the process for submitting pull requests.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Chat Interface

The chat interface is a crucial component of the HairCare+ solution, providing real-time communication between patients and doctors.

### Existing Features
- Real-time messaging with instant updates
- Message reply functionality with preview
- Timestamp display for each message
- Keyboard-aware interface that adjusts automatically
- Support for long messages with auto-expanding input
- Visual feedback for message status
- Message history persistence
- Photo sharing capabilities (camera and gallery)
- Message grouping by sender
- Online/Offline status indication
- Automatic scrolling to latest messages
- Message reply preview with truncation
- Smart response system with contextual replies
- Input field with attachment options
- Dynamic send button visibility
- Message bubble styling with proper alignment
- Support for both light and dark themes

### Missing/Planned Features
- Voice messages
- Video messages
- File attachments (documents, PDFs)
- Message delivery status (sent, delivered, read)
- Typing indicators
- Message reactions/emojis
- Message search functionality
- Message forwarding
- Message deletion
- Message editing
- Rich text formatting in messages
- Link preview
- Group chat support
- Message pinning
- Message bookmarks
- Offline message queue
- End-to-end encryption indicators
- Message translation
- Voice/Video call integration

### Chat Styling

#### Message Bubbles
- **Patient Messages**
  - Light theme: `#EAF4FC` (light blue)
  - Dark theme: `#1E2A35` (dark blue)
  - Corner radius: 18
  - Maximum width: 280
  - Right alignment

- **Doctor Messages**
  - Light theme: `#A0DAB2` (mint green)
  - Dark theme: `#4D7B63` (dark mint)
  - Corner radius: 18
  - Maximum width: 280
  - Left alignment

#### Typography
- Message content: 17sp
- Timestamp: 11sp
- Reply preview: 13sp
- Input field: 17sp
- Navigation buttons: 22sp

#### Reply Preview
- Background matches message style
- Opacity: 0.9
- Corner radius: 8
- Shows original message preview
- Truncates long content
- Clear button for canceling reply

#### Input Panel
- Light theme background: `#F7F7F7`
- Dark theme background: `#121212`
- Input field corner radius: 18
- Expandable input area (38-120px height)
- Attachment button: "+" symbol
- Send button: "↑" symbol
- Dynamic send button visibility

#### Navigation
- Back button: "←" symbol
- Doctor name: 18sp, bold
- Online status indicator
- Shadow effects on iOS

### Behavior
- Auto-scrolls to latest message
- Keyboard-aware layout adjustment
- Tap to reply to specific messages
- Message grouping by sender
- Smooth animations for state changes

