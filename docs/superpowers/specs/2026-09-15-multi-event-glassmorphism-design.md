# Sticky Note Premium — Multi-event & Glassmorphism Design

Date: 2026-09-15

## Goal

Upgrade Sticky Note Premium from a single fixed countdown to a local multi-event holiday/event manager. The main widget automatically displays the nearest upcoming one-time event, switches to the next event after the current target passes, allows manual previous/next browsing, and preserves expired events for later editing or deletion. Replace the pale-yellow timer box with a compact glassmorphism card that visually blurs the note background behind it.

## Event model and persistence

- Add `HolidayEvent` with `Id`, `Name`, and local `TargetDateTime`.
- `AppSettings.Events` stores all one-time events; expired events remain in the list.
- Existing legacy JSON containing `EventName` + `TargetDateTime` must migrate to a one-item `Events` list on load.
- Current settings for background, image, overlay, foreground theme, topmost, and window geometry remain compatible.
- Events are persisted to `%LocalAppData%\StickyNotePremium\settings.json`.

## Automatic and manual selection

- AUTO mode chooses the event whose `TargetDateTime` is strictly greater than `DateTime.Now` and nearest in time.
- The one-second refresh loop reevaluates AUTO selection, so after an event expires the next future event becomes active without restart.
- If no future event exists in AUTO mode, show a zero countdown and a `Không còn sự kiện sắp tới` state; keep all events in settings.
- Previous/next buttons switch to manual mode and browse all events chronologically, including expired events.
- An `AUTO` control returns to nearest-upcoming selection.
- Manual selection remains stable while the timer updates.

## Event-management UI

The settings popup contains an event section above appearance settings:

- List all events sorted by target date/time.
- Selecting a list item loads it into editor fields.
- `+ Sự kiện mới` clears the editor for adding a new event.
- Editable fields: name, date, and `HH:mm` time.
- `Lưu` inserts a new event or replaces the selected event while preserving its ID.
- `Xóa` removes the selected event.
- Invalid name/date/time keeps persisted data unchanged and surfaces a validation message.

Only one-time fixed dates are in scope for this version. Recurring annual rules remain out of scope.

## Main horizontal widget

Keep the wide/short borderless layout and reference-inspired hierarchy:

- summary text at top
- centered navigation row `‹  EVENT NAME  ›`
- compact glass countdown card in the center
- target-date line below
- AUTO indicator/control
- existing top-right pin/settings/minimize/close controls

## Glassmorphism timer card

- Replace the opaque pale-yellow timer panel.
- Use a centered, rounded (`16–20px`) card.
- Sample the full background layer into the card with a WPF `VisualBrush` aligned to the centered background visual.
- Apply `BlurEffect` to the sampled layer, then overlay a translucent white tint, subtle white border, soft shadow, and gentle separators.
- Countdown values and labels use the current foreground theme to remain readable.
- When only a solid background is used, the translucent glass layers remain visually coherent even though blur has little visible texture.
- No third-party library is required.

## Architecture

- `Models/HolidayEvent.cs`: serializable one-time event record.
- `Models/AppSettings.cs`: settings + event list.
- `Services/EventSelectionService.cs`: deterministic nearest-upcoming and chronological helper logic.
- `Services/SettingsService.cs`: JSON persistence, sanitization, and legacy migration.
- `ViewModels/MainViewModel.cs`: AUTO/manual selection, event editor state, countdown state, appearance state.
- `MainWindow.xaml`: horizontal glass UI and multi-event settings list.
- `MainWindow.xaml.cs`: window-only behavior and button event forwarding.
- `verification/Program.cs`: behavior checks for selection and settings persistence/migration.
- `verification/verify_source.py`: static regression checks runnable in environments without .NET SDK.

## Verification

Verify at minimum:

- nearest future event is selected independent of list order
- expired events are ignored by AUTO but remain in persisted list
- no-future-event state is safe and zeroed
- manual previous/next can browse expired/future events
- adding/editing/deleting events updates persisted settings
- legacy single-event JSON migrates without losing appearance settings
- timer automatically reselects after current event expires
- XAML contains a glass card using `VisualBrush` and `BlurEffect`
- background image, color, overlay, foreground, topmost, resize, and local persistence still work
- source passes static verification; compile/build must be verified with .NET 8 SDK on Windows when available
