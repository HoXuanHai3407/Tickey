# Multi-event Countdown & Glassmorphism Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Upgrade Sticky Note Premium to manage many fixed one-time events, automatically count down to the nearest upcoming event, support manual browsing, and render the center timer as a glassmorphism card over the configured background.

**Architecture:** Introduce a serializable `HolidayEvent` list and deterministic `EventSelectionService`; keep persistence/migration in `SettingsService`; let `MainViewModel` own selection mode, event editor state, timer refresh, and appearance. The WPF shell remains thin and forwards window/file-picker/button interactions to the view model.

**Tech Stack:** C# 12, .NET 8 WPF, `System.Text.Json`, `ObservableCollection<T>`, `VisualBrush`, `BlurEffect`; no third-party runtime packages.

**Spec:** `docs/superpowers/specs/2026-09-15-multi-event-glassmorphism-design.md`

## Global Constraints

- Keep the existing wide/short borderless WPF widget.
- Events are fixed one-time targets only; recurring annual rules are out of scope.
- AUTO mode selects the nearest event with `TargetDateTime > DateTime.Now` and reevaluates every second.
- Expired events remain persisted and editable/deletable.
- Manual previous/next browsing includes all events sorted chronologically and stays manual until AUTO is pressed.
- Existing single-event JSON must migrate to the new event list.
- Preserve existing background image/color, overlay, foreground theme, topmost, resize, and geometry behavior.
- Glassmorphism must use built-in WPF only.
- Do not claim a successful .NET build unless `dotnet build` is actually run successfully.

---

### Task 1: Event model and deterministic selection

**Files:**
- Create: `Models/HolidayEvent.cs`
- Create: `Services/EventSelectionService.cs`
- Modify: `Models/AppSettings.cs`
- Modify: `verification/Program.cs`
- Modify: `verification/verify_source.py`

**Interfaces:**
- Produces: `HolidayEvent.Id`, `HolidayEvent.Name`, `HolidayEvent.TargetDateTime`, `HolidayEvent.Clone()`.
- Produces: `EventSelectionService.GetNearestUpcoming(IEnumerable<HolidayEvent>, DateTime) -> HolidayEvent?`.
- Produces: `EventSelectionService.GetChronological(IEnumerable<HolidayEvent>) -> List<HolidayEvent>`.
- Produces: `AppSettings.Events : List<HolidayEvent>`.

- [ ] Add failing static checks requiring the new model/service/list and verification behavior vectors.
- [ ] Run `python3 verification/verify_source.py` and confirm failures are caused by the missing multi-event APIs.
- [ ] Implement the event model, settings list, and pure selection service.
- [ ] Extend `verification/Program.cs` with nearest-future, expired-ignore, and chronological-order assertions.
- [ ] Re-run static verification and confirm Task 1 checks pass.

### Task 2: Settings migration and sanitization

**Files:**
- Modify: `Services/SettingsService.cs`
- Modify: `verification/Program.cs`
- Modify: `verification/verify_source.py`

**Interfaces:**
- Consumes: `AppSettings.Events`.
- Produces: legacy migration from `EventName`/`TargetDateTime` JSON to a single `HolidayEvent`.
- Produces: sanitized event IDs, names, date range, duplicate-ID repair, and chronological persisted list.

- [ ] Add failing checks for legacy JSON property detection and event-list sanitization.
- [ ] Run static verification and confirm those new checks fail before implementation.
- [ ] Implement migration using `JsonDocument` only when the current JSON has no usable `Events` collection.
- [ ] Sanitize event records while preserving expired events; generate IDs for missing/duplicate IDs and discard only invalid/blank records.
- [ ] Extend the C# verification program with round-trip list persistence and legacy migration checks.
- [ ] Re-run static verification.

### Task 3: AUTO/manual view-model and event editor

**Files:**
- Modify: `ViewModels/MainViewModel.cs`
- Modify: `verification/verify_source.py`

**Interfaces:**
- Produces: `ObservableCollection<HolidayEvent> Events`.
- Produces: `HolidayEvent? CurrentEvent`, `CurrentEventName`, `IsAutoMode`, `SelectionModeText`.
- Produces editor state: `SelectedManagedEvent`, `EditEventName`, `EditTargetDate`, `EditTargetTimeText`.
- Produces actions: `UseAutoMode()`, `ShowPreviousEvent()`, `ShowNextEvent()`, `BeginNewEvent()`, `SaveEditedEvent() -> bool`, `DeleteSelectedEvent() -> bool`.

- [ ] Add failing static checks for AUTO/manual/editor API and selection refresh calls.
- [ ] Run static verification to prove the new surface is not present yet.
- [ ] Refactor countdown refresh so AUTO reevaluates nearest-upcoming every tick and manual mode keeps its selection.
- [ ] Implement chronological previous/next browsing and no-future safe state.
- [ ] Implement add/edit/delete event workflow with validation and persistence synchronization.
- [ ] Preserve background and appearance APIs from the existing view model.
- [ ] Re-run static verification.

### Task 4: Horizontal multi-event UI and glass card

**Files:**
- Modify: `MainWindow.xaml`
- Modify: `MainWindow.xaml.cs`
- Modify: `verification/verify_source.py`

**Interfaces:**
- Consumes: new view-model selection/editor APIs.
- Produces: previous/next/AUTO controls and event-management settings section.
- Produces: timer glass card using `VisualBrush` + `BlurEffect` + translucent tint/border/shadow.

- [ ] Add failing XAML/source checks for navigation controls, event list/editor buttons, `VisualBrush`, `BlurEffect`, translucent glass layers, and code-behind handlers.
- [ ] Run static verification and confirm failures.
- [ ] Rebuild the center countdown area as a compact glass card while keeping the horizontal layout.
- [ ] Add event title navigation and AUTO status/control to the main widget.
- [ ] Replace the single-event settings fields with a list/editor for many events while retaining appearance settings.
- [ ] Add code-behind handlers that call the view-model actions and keep drag interactions from swallowing list/editor controls.
- [ ] Parse XAML and re-run static verification.

### Task 5: Documentation, regression verification, packaging

**Files:**
- Modify: `README.md`
- Modify: `verification/verify_source.py`
- Package: `/mnt/data/StickyNotePremium_MultiEvent_Glass.zip`

**Interfaces:**
- Produces: updated user instructions for multi-event AUTO/manual behavior and event editing.
- Produces: source archive without transient `bin/`, `obj/`, `.git/`, or `__pycache__/` folders.

- [ ] Update README with event management, AUTO/manual switching, glassmorphism notes, legacy migration, CLI build, and publish instructions.
- [ ] Run `python3 verification/verify_source.py` and require zero failures.
- [ ] If `dotnet` exists, run `dotnet build StickyNotePremium.csproj -c Release`; otherwise record the SDK limitation without claiming build success.
- [ ] Validate XML files, inspect the archive file list, and create the final ZIP.
