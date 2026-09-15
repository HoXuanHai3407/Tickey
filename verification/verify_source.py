from __future__ import annotations

import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
failures: list[str] = []
checks = 0


def require_file(path: str) -> str:
    global checks
    checks += 1
    p = ROOT / path
    if not p.exists():
        failures.append(f"missing file: {path}")
        return ""
    return p.read_text(encoding="utf-8")


def require(text: str, pattern: str, label: str, regex: bool = False) -> None:
    global checks
    checks += 1
    ok = re.search(pattern, text, re.S) is not None if regex else pattern in text
    if not ok:
        failures.append(label)



def forbid(text: str, pattern: str, label: str, regex: bool = False) -> None:
    global checks
    checks += 1
    found = re.search(pattern, text, re.S) is not None if regex else pattern in text
    if found:
        failures.append(label)

def parse_xml(path: str) -> None:
    global checks
    checks += 1
    try:
        ET.parse(ROOT / path)
    except Exception as exc:
        failures.append(f"XML parse failed for {path}: {exc}")


csproj = require_file("StickyNotePremium.csproj")
require(csproj, "net8.0-windows", "csproj must target net8.0-windows")
require(csproj, "<UseWPF>true</UseWPF>", "csproj must enable WPF")
require(csproj, '<Compile Remove="verification/**/*.cs" />', "main WPF project must exclude verification C# sources")

event_model = require_file("Models/HolidayEvent.cs")
for token in ["class HolidayEvent", "Id", "Name", "TargetDateTime", "Clone()"]:
    require(event_model, token, f"HolidayEvent missing {token}")

settings = require_file("Models/AppSettings.cs")
for token in ["List<HolidayEvent>", "Events", "BackgroundColor", "BackgroundImagePath", "BackgroundImageOpacity",
              "OverlayOpacity", "ImageStretchMode", "ForegroundTheme", "AlwaysOnTop", "WindowLeft", "WindowTop",
              "WindowWidth", "WindowHeight", "CreateDefault"]:
    require(settings, token, f"AppSettings missing {token}")

snapshot = require_file("Models/CountdownSnapshot.cs")
for token in ["Days", "Hours", "Minutes", "Seconds", "CalendarDays", "ApproxMonths", "ApproxDays", "HasReached"]:
    require(snapshot, token, f"CountdownSnapshot missing {token}")

calculator = require_file("Services/CountdownCalculator.cs")
require(calculator, "Calculate(DateTime now, DateTime target)", "CountdownCalculator.Calculate API missing")
require(calculator, "Math.Ceiling", "countdown summary should use Math.Ceiling")
require(calculator, "30.44", "month approximation should use 30.44-day average")
require(calculator, "target <= now", "past targets should clamp at zero")

selector = require_file("Services/EventSelectionService.cs")
for token in ["GetNearestUpcoming", "GetChronological", "TargetDateTime > now", "OrderBy"]:
    require(selector, token, f"EventSelectionService missing {token}")

svc = require_file("Services/SettingsService.cs")
require(svc, "using System.IO;", "SettingsService must explicitly import System.IO")
for token in ["Environment.SpecialFolder.LocalApplicationData", "settings.json", "JsonSerializer", "JsonDocument",
              "EventName", "TargetDateTime", "MigrateLegacyEvent", "SanitizeEvents", "HashSet<string>",
              "Load()", "Save(AppSettings settings)", ".tmp", "File.Move"]:
    require(svc, token, f"SettingsService missing {token}")
require(svc, "catch", "SettingsService.Load should be exception-safe")

vm = require_file("ViewModels/MainViewModel.cs")
require(vm, "using System.IO;", "MainViewModel must explicitly import System.IO")
for token in ["ObservableCollection<HolidayEvent>", "Events", "CurrentEvent", "CurrentEventName", "IsAutoMode",
              "SelectionModeText", "SelectedManagedEvent", "EditEventName", "EditTargetDate", "EditTargetTimeText",
              "UseAutoMode", "ShowPreviousEvent", "ShowNextEvent", "BeginNewEvent", "SaveEditedEvent",
              "DeleteSelectedEvent", "GetNearestUpcoming", "DispatcherTimer", "TimeSpan.FromSeconds(1)",
              "RefreshCountdown", "SummaryText", "MonthSummaryText", "TargetDateText", "BackgroundBrush",
              "OverlayBrush", "ForegroundBrush", "IsSettingsOpen", "AlwaysOnTop", "BackgroundColor",
              "BackgroundImagePath", "BackgroundImageOpacity", "OverlayOpacity", "ImageStretchMode",
              "ForegroundTheme", "ValidationMessage", "SettingsChanged", "SetBackgroundImage",
              "ClearBackgroundImage", "BitmapCacheOption.OnLoad"]:
    require(vm, token, f"MainViewModel missing {token}")

xaml = require_file("MainWindow.xaml")
for token in ["WindowStyle=\"None\"", "AllowsTransparency=\"True\"", "SettingsPopup", "Events",
              "SelectedManagedEvent", "EditEventName", "EditTargetDate", "EditTargetTimeText",
              "NewEventButton_Click", "SaveEventButton_Click", "DeleteEventButton_Click", "PreviousEventButton_Click",
              "NextEventButton_Click", "AutoModeButton_Click", "VisualBrush", "BlurEffect", "GlassCountdownCard",
              "BackgroundVisual", "BrowseBackground_Click", "ClearBackground_Click", "BackgroundImageOpacity",
              "OverlayOpacity", "ImageStretchMode", "ForegroundTheme", "AlwaysOnTop", "ResizeThumb",
              "SummaryText", "MonthSummaryText", "TargetDateText", "Days", "Hours", "Minutes", "Seconds"]:
    require(xaml, token, f"MainWindow.xaml missing {token}")
require(xaml, "Orientation=\"Horizontal\"", "countdown should use horizontal layout")
require(xaml, "#22FFFFFF", "glass card should include a translucent white tint")

codebehind = require_file("MainWindow.xaml.cs")
for token in ["DragMove", "OpenFileDialog", "WindowState.Minimized", "SystemParameters.VirtualScreenLeft",
              "PersistSettings", "ResizeThumb_DragDelta", "ColorPreset_Click", "PreviousEventButton_Click",
              "NextEventButton_Click", "AutoModeButton_Click", "NewEventButton_Click", "SaveEventButton_Click",
              "DeleteEventButton_Click"]:
    require(codebehind, token, f"MainWindow.xaml.cs missing {token}")

for xml_file in ["App.xaml", "MainWindow.xaml"]:
    if (ROOT / xml_file).exists():
        parse_xml(xml_file)

verification_project = require_file("verification/StickyNotePremium.Verification.csproj")
for token in [r"Models\HolidayEvent.cs", r"Services\EventSelectionService.cs", r"Models\AppSettings.cs", r"Services\SettingsService.cs"]:
    require(verification_project, token, f"verification project missing linked source {token}")

verification_program = require_file("verification/Program.cs")
for token in ["GetNearestUpcoming", "expired", "legacy", "Events.Count", "SettingsService", "using System.IO;"]:
    require(verification_program, token, f"verification Program missing {token}")

readme = require_file("README.md")
for token in ["dotnet build", "%LocalAppData%", "AUTO", "nhiều", "Glassmorphism", "Sự kiện mới", "win-x64"]:
    require(readme, token, f"README missing {token}")

build = require_file("build-release.ps1")
require(build, "dotnet publish", "build-release.ps1 should publish the app")
require(build, "win-x64", "build-release.ps1 should target win-x64")

# Regression: System.Threading.CountdownEvent is implicitly imported by .NET 8.
# The app model must use a distinct name to avoid CS0104 ambiguity.
production_cs = "\n".join(
    p.read_text(encoding="utf-8")
    for p in ROOT.rglob("*.cs")
    if "verification" not in p.parts and "obj" not in p.parts and "bin" not in p.parts
)
forbid(production_cs, r"\bCountdownEvent\b", "app model name must not collide with System.Threading.CountdownEvent", regex=True)

if failures:
    print(f"FAILED: {len(failures)} failure(s), {checks} checks")
    for item in failures:
        print(f" - {item}")
    sys.exit(1)

print(f"PASS: {checks} source/XAML checks")
