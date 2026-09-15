# Sticky Note Premium Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a complete .NET 8 WPF sticky-note countdown app with a horizontal reference-style layout, configurable event/date/time, solid or image backgrounds, always-on-top behavior, and local JSON persistence.

**Architecture:** Keep the WPF shell thin. `MainWindow` owns window-specific actions (drag, resize, file picker, minimize/close, monitor-bound correction); `MainViewModel` owns countdown/UI state; `CountdownCalculator` is a deterministic pure service; `SettingsService` owns resilient JSON persistence. A single mutable `AppSettings` instance is shared by the view model and window so state can be saved without a database.

**Tech Stack:** C# 12, .NET 8, WPF, `System.Text.Json`, `Microsoft.Win32.OpenFileDialog`; no third-party runtime dependencies.

**Spec:** `docs/superpowers/specs/2026-09-15-sticky-note-premium-design.md`

## Global Constraints

- Application name: `Sticky Note Premium`.
- Target framework: `net8.0-windows` with WPF enabled.
- Main widget layout is wide/short and visually close to the provided red reference image.
- Configuration controls stay hidden until the settings button is opened.
- Main countdown shows whole days plus remaining hours/minutes/seconds, never negative.
- Summary day count uses calendar/ceiling-style presentation while timer days use whole elapsed 24-hour blocks.
- Background supports solid color and local image file; missing/unreadable images fall back safely to the solid color.
- Settings persist to `%LocalAppData%\StickyNotePremium\settings.json`.
- No database, cloud, account integration, recurring reminders, tray notifications, or installer in v1.

---

### Task 1: Project scaffold, settings model, countdown calculator

**Files:**
- Create: `StickyNotePremium.csproj`
- Create: `App.xaml`
- Create: `App.xaml.cs`
- Create: `Models/AppSettings.cs`
- Create: `Models/CountdownSnapshot.cs`
- Create: `Services/CountdownCalculator.cs`
- Create: `verification/test_vectors.json`
- Create: `verification/verify_source.py`

**Interfaces:**
- Produces: `AppSettings.CreateDefault()`.
- Produces: `CountdownCalculator.Calculate(DateTime now, DateTime target) -> CountdownSnapshot`.
- Produces: `CountdownSnapshot` fields `Days`, `Hours`, `Minutes`, `Seconds`, `CalendarDays`, `ApproxMonths`, `ApproxDays`, `HasReached`.

- [ ] **Step 1: Write the failing source-verification checks**

Create `verification/verify_source.py` to assert that the expected model/calculator files and public API signatures exist. Run it before creating the production files and confirm it fails because the files are missing.

- [ ] **Step 2: Create the WPF project and model files**

Use SDK-style `Microsoft.NET.Sdk`, target `net8.0-windows`, set `<UseWPF>true</UseWPF>`, nullable and implicit usings enabled. `AppSettings` contains event/target/background/topmost/window geometry fields and a red/yellow sample-inspired default.

- [ ] **Step 3: Implement the deterministic countdown math**

Clamp target times at zero; use `Math.Floor(remaining.TotalDays)` for timer days, modulo components for hours/minutes/seconds, `Math.Ceiling(remaining.TotalDays)` for summary calendar days, and average-month approximation `30.44` for the optional month text.

- [ ] **Step 4: Re-run source verification**

Expected: model/calculator structure checks pass.

### Task 2: Resilient local settings persistence

**Files:**
- Create: `Services/SettingsService.cs`
- Modify: `verification/verify_source.py`

**Interfaces:**
- Produces: `SettingsService.SettingsPath`.
- Produces: `SettingsService.Load() -> AppSettings`.
- Produces: `SettingsService.Save(AppSettings settings)`.

- [ ] **Step 1: Add failing checks for persistence API and LocalAppData path usage**

Verify the source includes `Environment.SpecialFolder.LocalApplicationData`, `settings.json`, `JsonSerializer`, exception-safe load, and temporary-file replacement/save semantics.

- [ ] **Step 2: Implement `SettingsService`**

Create `%LocalAppData%\StickyNotePremium`; deserialize case-insensitively; return defaults for missing/corrupt files; sanitize invalid geometry and opacity values; save to a temp file then replace the target.

- [ ] **Step 3: Re-run verification**

Expected: persistence source checks pass.

### Task 3: View-model countdown and background state

**Files:**
- Create: `ViewModels/MainViewModel.cs`
- Modify: `verification/verify_source.py`

**Interfaces:**
- Consumes: shared `AppSettings`, `CountdownCalculator`.
- Produces bindable properties: `EventName`, `TargetDate`, `TargetTimeText`, `Days`, `Hours`, `Minutes`, `Seconds`, `SummaryText`, `MonthSummaryText`, `TargetDateText`, `BackgroundBrush`, `OverlayBrush`, `ForegroundBrush`, `CountdownPanelBrush`, `IsSettingsOpen`, `AlwaysOnTop`, `BackgroundColor`, `BackgroundImagePath`, `BackgroundImageOpacity`, `OverlayOpacity`, `ImageStretchMode`, `ForegroundTheme`, `ValidationMessage`.
- Produces events: `SettingsChanged`, `BackgroundImageLoadFailed`.
- Produces methods: `Start()`, `Stop()`, `RefreshCountdown()`, `SetBackgroundImage(string path)`, `ClearBackgroundImage()`.

- [ ] **Step 1: Add failing checks for view-model API and one-second `DispatcherTimer`**

- [ ] **Step 2: Implement notification, timer, summary formatting, target validation, and settings mutation**

Format the Vietnamese bottom date with `vi-VN`. For invalid `HH:mm`, retain the prior target and set `ValidationMessage`. Raise `SettingsChanged` only when persisted state changes.

- [ ] **Step 3: Implement background brushes and theme behavior**

Load images using `BitmapImage` with `BitmapCacheOption.OnLoad`, create `ImageBrush`, map `UniformToFill`/`Uniform`/`Fill`, apply opacity, and fall back to the solid color on failure. Auto text is dark on bright solid colors and white when an image is active.

- [ ] **Step 4: Re-run verification**

Expected: view-model checks pass.

### Task 4: Horizontal WPF UI and borderless window behaviors

**Files:**
- Create: `MainWindow.xaml`
- Create: `MainWindow.xaml.cs`
- Modify: `verification/verify_source.py`

**Interfaces:**
- Consumes: `MainViewModel`, shared `AppSettings`, `SettingsService`.
- Produces: a borderless horizontal widget plus hidden settings flyout.

- [ ] **Step 1: Add failing XAML/source checks**

Require `WindowStyle="None"`, transparent shell, four horizontal countdown columns, settings panel, `DatePicker`, time `TextBox`, color editor, image browse/remove controls, opacity sliders, stretch/theme selectors, topmost toggle, drag region, toolbar, and resize thumb.

- [ ] **Step 2: Implement `MainWindow.xaml`**

Create a 790x220 default widget with red background, centered white summary text, pale-yellow timer panel, dark numbers/labels, target date line, compact top-right toolbar, and settings flyout that overlays rather than expands the normal countdown layout.

- [ ] **Step 3: Implement `MainWindow.xaml.cs`**

Wire drag, custom resize, minimize/close, settings toggle, color picker via text + preset swatches, image selection/removal, settings debounce-save, window geometry save, topmost update, and off-screen correction using `SystemParameters.VirtualScreen*`.

- [ ] **Step 4: Re-run verification**

Expected: UI/behavior structure checks pass and XAML parses as XML.

### Task 5: Documentation, build helper, final verification, packaging

**Files:**
- Create: `README.md`
- Create: `build-release.ps1`
- Modify: `verification/verify_source.py`
- Create: `StickyNotePremium.sln` if a .NET SDK is available; otherwise document opening the `.csproj` directly.

**Interfaces:**
- Produces user instructions for Visual Studio 2022 / .NET 8 Desktop Development.
- Produces one-command PowerShell publish helper.

- [ ] **Step 1: Document run/build/publish and settings location**

Include image-background usage, default controls, WebP decoder caveat, and the exact publish command for `win-x64` single-file framework-dependent or self-contained output.

- [ ] **Step 2: Run static verification**

Run `python3 verification/verify_source.py`. Expected: all checks pass.

- [ ] **Step 3: Run .NET build if SDK is present**

Run `dotnet build -c Release`. If `dotnet` is unavailable in the execution environment, record that limitation explicitly and do not claim a successful compile.

- [ ] **Step 4: Package source**

Create `/mnt/data/StickyNotePremium_Source.zip` containing the project, docs, verification script, and build helper, excluding transient build folders.
