# Sticky Note Premium — Design Specification

Date: 2026-09-15

## 1. Goal

Build a complete Windows desktop application named **Sticky Note Premium** using **C# + WPF + .NET 8**. The application is a borderless sticky-note style countdown widget that can stay on top of other windows, lets the user configure an event and target date/time, supports both solid-color and image backgrounds, and restores its state after restart.

## 2. Primary UI direction

The main window will use a **horizontal layout closely matching the user-provided reference image** rather than a tall card layout.

Default presentation:

- Wide, short borderless window.
- Countdown content centered horizontally.
- Background image fills the full note area, with optional overlay/tint for readability.
- First line: summary text, e.g. `Còn 144 ngày nữa đến tết nguyên đán năm 2027`.
- Second line: optional converted-month summary, e.g. `(Quy đổi thành tháng: 4 tháng 21 ngày)`.
- Center panel: four countdown values displayed in one row: `Ngày`, `Giờ`, `Phút`, `Giây`.
- Bottom line: target date/time, e.g. `Ngày diễn ra (theo dương lịch): Thứ 7, 06/02/2027`.
- Typography and spacing should closely follow the reference while remaining responsive if the window is resized.

The default view should stay visually clean. Configuration controls should not permanently occupy space in the countdown layout.

## 3. Window behavior

- `WindowStyle=None`, transparent/borderless chrome.
- Draggable by clicking and dragging empty areas or the title/drag region.
- Optional rounded corners and subtle shadow.
- `Topmost=true` by default, toggleable by the user.
- Minimize and close actions are available from a compact overlay/toolbar.
- Remembers window position and size.
- Restores to a visible monitor area if the saved position is off-screen.

## 4. Configuration UI

A small gear/settings button will open a compact settings panel or popup so the normal widget remains visually close to the reference image.

Settings include:

- Event name.
- Target date.
- Target time.
- Always-on-top toggle.
- Background mode: solid color or image.
- Solid background color.
- Select image from disk (`.jpg`, `.jpeg`, `.png`, `.bmp`, `.webp` where supported by WPF decoder availability).
- Remove/reset background image.
- Background image stretch mode: `UniformToFill`, `Uniform`, `Fill`.
- Background image opacity / overlay strength.
- Foreground text theme: light/dark/auto.

Changes should reflect immediately in the widget.

## 5. Countdown behavior

- Uses a `DispatcherTimer` ticking once per second.
- Computes remaining time from `DateTime.Now` to the configured target.
- Displays total whole days plus remaining hours/minutes/seconds.
- Values are always non-negative.
- At or after target time, display zeros and an event-reached state rather than negative time.
- Summary text above the timer is generated from the event name and remaining day count.
- Optional month conversion is a display approximation based on whole months/days remaining; it is not used for the actual countdown calculation.
- Bottom line formats the target date in Vietnamese culture.

## 6. Architecture

Use a lightweight MVVM structure:

- `MainWindow.xaml` — visual layout.
- `MainWindow.xaml.cs` — window-only behaviors such as dragging, file picker, minimize/close, and monitor-bound restoration.
- `ViewModels/MainViewModel.cs` — countdown state, settings-bound properties, commands/state updates.
- `Models/AppSettings.cs` — serializable settings model.
- `Services/SettingsService.cs` — JSON persistence.
- `Converters/` — small WPF converters if needed for visibility, brushes, or image handling.

No database and no server dependency.

## 7. Local persistence

Settings file:

`%LocalAppData%\StickyNotePremium\settings.json`

Persist at least:

- event name
- target date/time
- background color
- background image path
- background image opacity / overlay
- image stretch mode
- foreground theme
- always-on-top
- window left/top/width/height

Save after meaningful setting changes and on application/window close.

If settings are missing or corrupt, fall back to safe defaults instead of crashing.

If a saved background image is no longer available, fall back to the selected background color.

## 8. Styling

The default sample theme will visually resemble the supplied reference:

- red background / red-toned image
- white summary and target-date text
- pale-yellow countdown panel
- dark countdown numbers and labels
- strong centered hierarchy

The user-selected image can replace the default background while preserving readable text through an optional overlay.

## 9. Error handling

- Invalid date/time input: retain the last valid target and indicate validation error in settings.
- Unreadable image: show a small non-blocking error and keep the previous background.
- Corrupt settings JSON: ignore it, load defaults, and overwrite on next valid save.
- Off-screen saved window position: move to the primary working area.

## 10. Testing / verification

Verify:

- project builds on .NET 8 WPF
- countdown updates every second
- day/hour/minute/second math is correct
- past target clamps to zero
- settings persist and restore
- image background select/remove works
- color changes work
- topmost toggle works
- window drag/minimize/close works
- invalid/missing image paths do not crash the app
- saved off-screen window coordinates are corrected on startup

## 11. Out of scope for first version

- cloud sync
- Microsoft account integration
- multiple notes/windows
- recurring reminders
- system tray scheduling/notifications
- installer/MSIX packaging

These can be added later without changing the core countdown/settings architecture.
