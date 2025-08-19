## HairCare+ Offline-first: Simplification Plan (Aug 2025)

### Goals
- Keep the server stateless/ephemeral – act only as a relay with short TTL.
- Store all durable data on clients (Clinic/Patient) with local SQLite + Outbox.
- Deliver updates via DeliveryQueue + SignalR; ACK drives cleanup.
- Minimize drift and fragile logic; prefer simple, deterministic flows.

### Scope
- Photo reports (single and atomic 3-photo sets)
- Chat messages
- Photo comments
- Restrictions and calendar (events/tasks)

### Decisions
1) Ephemeral server storage
   - Do not persist ChatMessages, PhotoComments, CalendarTasks on the server.
   - Accept payloads via `/sync/batch`, enqueue into `DeliveryQueue` with TTL, and broadcast SignalR events.
   - Server cleans files only after all receivers ACK or TTL expires.

2) Atomic PhotoReportSet delivery
   - Patient groups 3 photos into `PhotoReportSetDto` and enqueues a single Outbox item.
   - Sync attempts to ensure each item has HTTP URL (upload missing), otherwise defers.
   - Server enqueues one `DeliveryQueue` packet and emits `PhotoReportSetAdded`.
   - Clinic caches files locally, persists three `PhotoReportEntity`, ACKs by packet Id.

3) Comments delivery
   - Do not store comments on the server; deliver as `DeliveryQueue` packets + SignalR `PhotoCommentAdded` when patientId is known.
   - Clients persist comments locally and surface immediately in UI.

4) Chat delivery
   - Real-time via SignalR; for offline peers also enqueue `DeliveryQueue` packets with short TTL.
   - No long-term persistence on the server.

5) Calendar & restrictions
   - Restrictions: server relays only (no persistence). Already implemented; keep as is.
   - Calendar tasks: relay-only (no DB writes). Use `ModifiedAtUtc` or server monotonic `NewSyncVersion` to resolve conflicts client-side.

6) DX/UX improvements
   - Patient UI: show capture progress 1/3 → 2/3 → 3/3 before sending.
   - Assign proper `PhotoType` per template (Front/Top/Back) for correct grouping on Clinic side.
   - Add `x:DataType` in XAML to enable compiled bindings and remove warnings.

### Implementation steps
1) Server: BatchSyncCommand
   - Remove EF persistence of ChatMessages, PhotoComments, CalendarTasks.
   - Enqueue `DeliveryQueue` items for these entity types; keep existing logic for Restrictions, PhotoReport, PhotoReportSet.

2) Server: AddPhotoCommentCommand
   - Make it ephemeral: remove PhotoReport FK requirement and DB writes; just enqueue + broadcast.

3) Patient: PhotoCaptureViewModel
   - Map `PhotoReportItemDto.Type` from current `CaptureTemplate` (front/top/back).
   - Map saved local entity zone consistently.

4) Clinic/MAUI XAML
   - Add page-level `x:DataType` for `ChatPage` to reduce warnings.

5) Optional follow-ups
   - Move files to external object storage with TTL and pre-signed URLs to simplify server lifecycle.
   - Expand SignalR events for Calendar/Restriction changes.


