# Chat Module

> Version: September 2025 | .NET MAUI 9.0.51 SR

## TL;DR
Provides real-time messaging between patients and clinicians with text, media, threading, offline caching, and adaptive UI.

## Table of Contents
1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Functionality](#functionality)
4. [UI/UX Design](#uiux-design)
5. [Data Interactions](#data-interactions)
6. [Technical Implementation](#technical-implementation)
7. [Accessibility & Performance](#accessibility--performance)
8. [Security](#security)

## Overview
The Chat module enables real-time two-way communication between patients and healthcare providers. It supports text messaging, attachments, replies, and presence indicators.

## Architecture
Follows MVVM and Clean Architecture layers:
- **Models:** Data structures for chat messages
- **Views:** XAML UI components with styles and behaviors
- **ViewModels:** Business logic and commands
- **Converters:** Data transformations for UI bindings
- **Behaviors:** Custom touch and gesture handling

## Functionality
### Current Implementation
- Text messaging with sent/received styling and timestamps
- Reply functionality with quoted message previews
- Offline message caching and auto-scroll to newest message
- Swipe-to-edit and long-press for edit/delete
- Light/Dark theme support

### Planned Features
- Media attachments (camera/gallery)
- Delivery status indicators (sent, delivered, read)
- Message search, edit, and delete
- Rich notifications and deep linking

## UI/UX Design
### Color Scheme
- **Patient messages:** Light blue (#EAF4FC) on light, dark blue (#1E2A35) on dark
- **Clinician messages:** Green (#A0DAB2) on light, dark green (#4D7B63) on dark
- **Background:** #F7F7F7 (light) / #121212 (dark)
- **Text:** High-contrast black/white

### Layout
- Header with back button, clinician name, presence indicator
- `CollectionView` for message list with data templates
- Input area with attachment button, expanding text field, and send button
- Reply preview section above the input field

## Data Interactions
- **SendMessageCommand** → `ICommandBus`
- Incoming message events update ViewModel and local cache
- **GetMessagesQuery** retrieves message history
- Offline storage with retry logic on connectivity changes

## Technical Implementation
- **Models:** `ChatMessage`, `UserStatus`, `AppointmentInfo`, enums for message type/status
- **ViewModel:** `ChatViewModel` with observable collections and commands
- **Converters:** `MessageBackgroundConverter`, `TimestampConverter`
- **Behaviors:** `KeyboardBehavior` for keyboard management and scroll adjustment
- **DI Registration:** register chat services, handlers, and ViewModel in `MauiProgram`

## Accessibility & Performance
- `AutomationProperties` for screen-reader support
- Virtualized `CollectionView` with incremental loading
- UI updates on main thread via `Dispatcher`
- Resource cleanup in `OnDisappearing`

## Security
- End-to-end encryption support planned
- Secure storage of message history
- No sensitive patient data in notifications or logs 