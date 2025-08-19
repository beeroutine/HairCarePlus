# Clinic – Progress feed: in‑place doctor comment

## Summary
- Inline editor for the doctor’s comment lives inside the green bordered block on each progress card.
- Edit happens in place; no separate composer below the card.
- Offline‑first: comment is saved locally immediately and enqueued to Outbox; background sync delivers it when the network is available.

## UI & XAML
- View: `Common/Views/ProgressCardView.xaml`
  - One `Border` with header “Комментарий врача”.
  - Read mode: `Label` bound to `DoctorReportSummary`.
  - Edit mode: `Editor` + send button `↑` inside the same `Border`.
  - Visibility logic: the read‑only `Label` hides while the inline editor is visible (DataTrigger on `InlineEditPanel.IsVisible`).
  - Compiled bindings: card uses `x:DataType="models:ProgressFeedItem"` for performance and XamlC safety.

## ViewModel API
- File: `Features/Patient/ViewModels/PatientPageViewModel.cs`
- State
  - `CommentTarget: ProgressFeedItem?` – card currently being edited.
  - `CommentText: string` – text in the inline editor.
  - `IsSendEnabled: bool` – `CommentTarget != null && !string.IsNullOrWhiteSpace(CommentText)`.
- Commands
  - `StartCommentCommand(ProgressFeedItem)` – sets target, pre‑fills `CommentText` with existing note, shows editor.
  - `SendCommentCommand` (Async) – optimistic submit (see flow below).
  - `CancelCommentCommand` – resets state and hides editor.

## Offline‑first submit flow (optimistic)
1. User taps `↑`.
2. VM updates UI immediately:
   - Updates `Feed[index]` with the new text.
   - Clears `CommentText`, resets `CommentTarget`, hides editor.
3. VM calls `SubmitCommentAsync(reportId, text)` → `PhotoReportService.AddCommentAsync(...)`:
   - Writes to local SQLite (EF Core) table `PhotoComments`; also updates parent report’s `DoctorComment` for quick feed reads.
   - Invalidates in‑memory cache.
   - Enqueues an `OutboxItemDto { EntityType = "PhotoComment", Payload = dto }`.
   - Triggers `_syncService.SynchronizeAsync(...)` in background; no awaiting, no UI blocking.
4. Network is optional. If offline, the item stays in the Outbox and will be sent on the next sync; UI already shows the change.

## Error handling
- Local errors are logged via `ILogger<PatientPageViewModel>` and do not crash or block UI.
- No blocking dialogs on network failures; the edit remains visible locally until delivered.

## Why no double text during edit
- The previous read‑only text is hidden with a `DataTrigger` while the `InlineEditPanel` is visible, preventing overlay/duplication.

## Test checklist
- Tap existing comment → editor appears, old text hidden; send → label shows updated text.
- With airplane mode: send → text persists locally; upon restoring network, change is delivered.
- Reopen page → feed shows the latest local value.
