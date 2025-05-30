# Notifications Module

> Version: September 2025 | .NET MAUI 9.0.51 SR

## TL;DR
Schedules and manages local and calendar-triggered notifications for patient care reminders with offline-first reliability.

## Table of Contents
1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Functionality](#functionality)
4. [Technical Implementation](#technical-implementation)
5. [Platform Considerations](#platform-considerations)
6. [Integration](#integration)
7. [Security](#security)

## Overview
The Notifications module provides timely reminders for medications, procedures, photo submissions, and other postoperative care events.

## Architecture
Implements a service-based pattern:
- **Interfaces:** Contracts for notification scheduling and management
- **Services:** Business logic for scheduling, canceling, and handling notifications
- **Platform Implementations:** iOS/Android native APIs via DI

## Functionality
### Current Implementation
- Schedule, cancel, and cancel-all local notifications
- Attach optional data payloads to each notification
- Automatic notification creation for calendar events

### Planned Features
- Customizable repeat intervals and priority categories
- Interactive notification actions (mark as done, snooze)
- Rich media content and grouped notifications

## Technical Implementation
- **INotificationService:**
  - `ScheduleNotificationAsync(title, message, scheduledTime, data)`
  - `CancelNotificationAsync(notificationId)`
  - `CancelAllNotificationsAsync()`
- **NotificationService:** Debug stub; replace with platform-specific implementations
- **DI Registration:** `services.AddScoped<INotificationService, NotificationService>()`

## Platform Considerations
- **iOS:** `UNUserNotificationCenter` permissions, actions, and categories
- **Android:** `NotificationChannel` and `NotificationManager` with `AndroidX.Core.App.NotificationCompat`

## Integration
- **Calendar Module:** Sync scheduling and cancellation with calendar events
- **Chat Module:** Push notifications for new messages with deep links to chat

## Security
- No protected health information in notification bodies
- Encrypt sensitive payload data as needed
- Adhere to privacy policies and secure storage guidelines 