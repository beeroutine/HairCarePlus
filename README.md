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

## AI Model Agent Instructions

All tasks performed by AI model agents should adhere to the specified technology stack outlined in this document. Ensure that tasks are executed efficiently and without unnecessary work to maintain consistency and avoid errors.

### Third-Party Components

This project uses Syncfusion Essential Studio for MAUI components:
- The development version currently uses Syncfusion Trial Version
- Before releasing the app, you will need to either:
  - Obtain a Community License (free for individuals/small companies with <$1M revenue)
  - Purchase a commercial license
  - Replace Syncfusion components with standard MAUI alternatives
- To apply a license key, add `SyncfusionLicenseProvider.RegisterLicense("YOUR_LICENSE_KEY");` in MauiProgram.cs

### Package Version Consistency

Always maintain consistent versions across related packages:
- All Syncfusion packages must have the same version (currently 28.2.7)
- Ensure compatibility between .NET MAUI (8.0) and all third-party packages
- When updating packages, update all related packages together to avoid version conflicts

### Syncfusion Components Usage

When working with Syncfusion components in MAUI:
- Use the correct namespace prefix (lowercase): `xmlns:tabview="clr-namespace:Syncfusion.Maui.TabView;assembly=Syncfusion.Maui.TabView"`
- Use proper class names for Syncfusion components:
  - `SfTabView` instead of `TabView`
  - `SfTabItem` instead of `TabItem`
  - Use `<tabview:SfTabView.Items>` instead of `<tabview:SfTabView.TabItemsCollection>` and `<tabview:TabItemCollection>`
- Always verify compatibility between .NET MAUI version (8.0) and Syncfusion package versions
- Reference the current Syncfusion version in use: 28.2.7

### Performance Optimization

To avoid UI freezes and frame skipping:
- Avoid heavy processing in the main UI thread
- Use asynchronous methods with `async/await` pattern for network requests and database operations
- Implement lazy loading for views and components that are not immediately visible
- Consider using background threads for heavy calculations via `Task.Run()`
- Optimize image loading and processing, use caching when possible
- Keep UI component hierarchies flat when possible, avoid deep nesting
- Use virtualization for long lists (CollectionView instead of ListView)
- Implement proper view recycling patterns
- Minimize UI updates and property change notifications

### APK Size Optimization and Deployment

To reduce APK size and avoid storage issues during development/deployment:
- Use AOT compilation selectively or disable it for debug builds
- Trim unused assemblies using PublishTrimmed property
- Compress images and other assets before including them in the project
- For debug builds, consider disabling `EmbedAssembliesIntoApk` to use Shared Runtime
- Utilize `<AndroidPackageFormat>apk</AndroidPackageFormat>` instead of AAB format for debug builds
- Enable ProGuard to remove unused code
- Implement on-demand downloading of assets when applicable
- Consider using Dynamic Features for less common functionality

### Development Environment Setup

For optimal development experience:
- Configure Android emulators with at least 2GB RAM and 2GB internal storage
- When creating new AVDs, use x86 or x86_64 ABI for better performance
- Enable hardware acceleration in emulators (HAXM/Hypervisor)
- Regularly clear build caches (bin/obj folders)
- For physical devices, ensure they have sufficient storage (>1GB free)
- Set up multi-targeting configurations to build only for the platform you're testing
- Consider using Release configuration with debugging enabled for faster performance

## UI Design Guidelines

### Calendar Design for Male Audience 30+

The calendar interface follows specific design principles tailored for the target audience (men 30+):

#### Color Palette
- **Primary Colors**: Deep blue-purple (#6962AD) and its variations are used as the primary accent colors
- **Background Colors**: Clean, neutral backgrounds (light theme: #F8F8F8, dark theme: #1A1A1A)
- **Accent Colors**: Distinct colors for different event types (medications: green, restrictions: red, instructions: purple)
- **Text Colors**: High-contrast, readable text that maintains professionalism

#### Typography
- **Font Sizes**: Slightly larger than minimum (16-18px for regular text)
- **Font Weight**: Bold text for important information, regular for details
- **Font Family**: Sans-serif fonts for optimal readability

#### Visual Elements
- **Icons and Markers**: Minimalist dot indicators for event types
- **Progress Indicators**: Clear progress bars showing recovery stages
- **Current Day Highlighting**: Subtle background color highlighting for the current day

#### UI Principles
- **Minimalist Approach**: Only essential information is displayed
- **Information Hierarchy**: Clear visual distinction between different types of content
- **Reduced Visual Noise**: Clean interfaces without unnecessary decorative elements
- **Professional Appearance**: Structured layout conveying medical professionalism

These design principles create a calendar interface that is both functional and visually appropriate for the target demographic.

