# Sticky Note Premium

A sticky-note-style countdown application for Windows, built with **C# + WPF + .NET 8**. The main interface uses a horizontal layout inspired by the reference design, while the central countdown panel uses a **Glassmorphism** effect with a semi-transparent background, glass border, and a blurred version of the wallpaper behind it.

## Features

- Manage **multiple one-time holidays / events** in a single application.
- **AUTO mode** automatically selects the nearest upcoming event and refreshes every second.
- When an event has passed, AUTO mode automatically switches to the next upcoming event. Past events remain in the list so they can still be edited or deleted.
- Use the `‹` and `›` buttons to manually navigate through all events in chronological order, including past events.
- Click the `MANUAL • Click to AUTO` status button to return to AUTO mode.
- Borderless window with free dragging and `Always on top` support.
- Real-time countdown updated every second.
- Glassmorphism countdown panel using WPF `VisualBrush` + `BlurEffect`, with a semi-transparent glass layer, bright border, and soft shadow.
- Change the background color using a HEX value or preset colors.
- Select JPG/JPEG/PNG/BMP background images, with WebP support when a compatible Windows image decoder is available.
- Adjust background image opacity, dark overlay, image stretch mode (`UniformToFill`, `Uniform`, `Fill`), and text color (`Auto`, `Light`, `Dark`).
- Automatically saves the event list, background settings, window position, and window size.
- Automatically migrates legacy configuration containing only `EventName` + `TargetDateTime` into the new multi-event format.

## Requirements for Building from Source

You **do not need Visual Studio 2022**.

You only need Windows and the **.NET 8 SDK**. The project targets `net8.0-windows` and does not use any third-party NuGet packages.

Open PowerShell **in the directory containing `StickyNotePremium.csproj`**, then run:

```powershell
dotnet restore .\StickyNotePremium.csproj
dotnet build .\StickyNotePremium.csproj -c Release
dotnet run --project .\StickyNotePremium.csproj -c Release
```

If you run `dotnet build` from a parent directory that does not contain a `.csproj` or `.sln` file, MSBuild will report error `MSB1003`.

You can quickly verify that you are in the correct directory with:

```powershell
dir *.csproj
```

## Managing Multiple Events

Open `⚙` to access the **Events / Holidays** section.

1. Click **`+ New Event`**.
2. Enter the event name, select a date, and enter the time in `HH:mm` format.
3. Click **`Save`**.
4. To edit an event, select it from the list, update its information, then click `Save`.
5. To delete an event, select it and click `Delete`.

Events are sorted by date and time. Past events are not automatically deleted.

### AUTO Mode and Manual Navigation

- `AUTO • Nearest Event`: the app automatically selects the nearest event whose target time is still in the future.
- Once that target time has passed, the next refresh automatically switches to the next upcoming event.
- Clicking `‹` or `›` switches the app to manual mode.
- In manual mode, the app keeps displaying the selected event even as time passes.
- Click the status button below the countdown to return to AUTO mode.
- If there are no upcoming events, the countdown displays `00:00:00`, while past events remain available in the event list.

## Background Images and Glassmorphism

In Settings:

- `Choose Image...`: select a background image.
- `Remove Background Image`: return to a solid background color.
- `Background Image Opacity`: adjust the image opacity.
- `Dark Overlay`: improve text readability over bright images.
- The Glassmorphism countdown panel samples the background behind it using WPF `VisualBrush`, then applies `BlurEffect` and a semi-transparent white layer to create a seamless glass effect.

> **WebP:** WPF relies on image decoders available in Windows. JPG, PNG, and BMP work by default. WebP works only when Windows or an installed codec supports it. If WebP cannot be decoded, the application falls back to the configured background color and displays a non-blocking error.

## Configuration Data

The application stores its settings at:

```text
%LocalAppData%\StickyNotePremium\settings.json
```

The configuration includes:

- `Events` list
- Background color and image
- Background image opacity
- Dark overlay
- Image stretch mode
- Text color mode
- Always-on-top setting
- Window position
- Window size

If you are upgrading from an older version, a JSON configuration containing `EventName` and `TargetDateTime` will automatically be migrated into the first item of the `Events` collection when the application starts.

## Publishing the Windows Application

To create a framework-dependent `win-x64` build:

```powershell
.\build-release.ps1
```

To create a self-contained build that does not require the .NET Runtime to be installed on the target computer:

```powershell
.\build-release.ps1 -SelfContained
```

The output will be available in:

```text
publish\win-x64\
```

## Logic Verification

Run the verification project using the .NET SDK:

```powershell
dotnet run --project .\verification\StickyNotePremium.Verification.csproj
```

The verification project checks:

- Countdown calculations
- AUTO selection of the nearest upcoming event
- Skipping expired events during automatic selection
- Saving and loading multiple events
- Migration from the legacy single-event configuration

In environments where the .NET SDK is unavailable, you can run the static regression checks with:

```powershell
python verification\verify_source.py
```
