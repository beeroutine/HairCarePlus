# Progress Module

> Version: June 2025 | .NET MAUI 9.0.51 SR

## TL;DR
Provides a visual timeline of patient recovery with daily photos, AI analysis, and clinician comments, enriched by a year-view header and interactive restrictions.

## Table of Contents
1. [Purpose](#purpose)
2. [Features](#features)
3. [UI Design](#ui-design)
4. [Data Flow](#data-flow)
5. [Technical Implementation](#technical-implementation)
6. [Accessibility & Performance](#accessibility--performance)
7. [Future Refactoring](#future-refactoring)
8. [Testing & Review](#testing--review)

## Purpose
The Progress module displays a scrollable feed of daily recovery photos, highlighting progress with an interactive year timeline and upcoming restrictions.

## Features
- Sticky year timeline header with interactive markers
- Horizontal restriction timers with visual status indicators
- Vertical feed of `ProgressCardView` displaying:
  - Photo carousel with tap-to-fullscreen, pinch-zoom, and share
  - AI analysis score and feedback
  - Clinician comments
- Pull-to-refresh and floating action button for adding new photos

## UI Design
### Year Timeline
- Minimalist line with progress segments colored by age (0–1m, 1–3m, 3–6m, 6–12m)
- Tap markers for popups showing day-of-year and status

### Restriction Timers
- Horizontal `CollectionView` with circular icons and days-remaining labels
- Active (>1d), Soon (≤1d), Completed states styled via `Border` and theme resources

### ProgressCardView
- `CarouselView` for daily photos (AspectFill, corner radius 12px)
- Overlay `Day {DayNumber}` and date label
- AI block (`Border`, theme-aware background, padded content)
- Clinician block similar to AI block

## Data Flow
| Trigger              | Query/Message                | Updates              |
|----------------------|------------------------------|----------------------|
| App start/Refresh    | `GetProgressFeedQuery`       | Header + Feed        |
| New photo saved      | `PhotoCapturedMessage`       | Adds new ProgressCard|
| Restriction changed  | `RestrictionsChangedMessage` | Updates Timers       |
| Pull-to-Refresh      | Both queries                 | Header + Feed        |

## Technical Implementation
- **Views:** `ProgressPage.xaml`, `YearTimelineView`, `RestrictionTimersView`, `ProgressCardView`
- **ViewModel:** `ProgressViewModel` with observable collections and commands
- **CQRS:** Query and command buses with handlers (`GetProgressFeedQuery`, `PhotoCapturedMessageHandler`, etc.)
- **Services:** DI registration for message handlers and queries in `MauiProgram`

## Accessibility & Performance
- Virtualized `CollectionView` and lazy loading of images
- High-contrast color resources for readability
- `AutomationProperties` for UI elements
- UI updates via `Dispatcher` and minimal re-rendering

## Future Refactoring
- Extract UI component interfaces (`IYearTimelineView`, `IProgressCardView`) for testability
- Move touch and animation logic into a dedicated service
- Centralize dimensions and style resources in `Dimensions.xaml` and `Colors.xaml`
- Optimize image caching and lazy loading strategy

## Testing & Review
- Unit tests for `ProgressViewModel` and message handlers
- UI tests for key interactions using MAUI.Testing framework
- Performance benchmarks for feed rendering and animations

---
