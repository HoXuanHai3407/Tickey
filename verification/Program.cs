using System.IO;
using StickyNotePremium.Models;
using StickyNotePremium.Services;

var calculator = new CountdownCalculator();
var selector = new EventSelectionService();
var failures = new List<string>();

void Expect(bool condition, string message)
{
    if (!condition)
    {
        failures.Add(message);
    }
}

var now = new DateTime(2026, 9, 15, 10, 0, 0, DateTimeKind.Local);
var elapsed = calculator.Calculate(now, now.AddSeconds(-1));
Expect(elapsed.HasReached, "past target should be reached");
Expect(elapsed.Days == 0 && elapsed.Hours == 0 && elapsed.Minutes == 0 && elapsed.Seconds == 0,
    "past target should clamp every countdown component to zero");

var exact = calculator.Calculate(now, now.AddDays(1).AddHours(2).AddMinutes(3).AddSeconds(4));
Expect(!exact.HasReached, "future target should not be reached");
Expect(exact.Days == 1 && exact.Hours == 2 && exact.Minutes == 3 && exact.Seconds == 4,
    "countdown components should split remaining seconds");

var expired = new HolidayEvent { Id = "expired", Name = "Đã qua", TargetDateTime = now.AddDays(-10) };
var later = new HolidayEvent { Id = "later", Name = "Sau", TargetDateTime = now.AddDays(20) };
var nearest = new HolidayEvent { Id = "nearest", Name = "Gần nhất", TargetDateTime = now.AddDays(2) };
var unordered = new[] { later, expired, nearest };
Expect(selector.GetNearestUpcoming(unordered, now)?.Id == "nearest", "AUTO should choose nearest future event");
Expect(selector.GetNearestUpcoming(new[] { expired }, now) is null, "AUTO should return null when no future event exists");
var chronological = selector.GetChronological(unordered);
Expect(chronological[0].Id == "expired" && chronological[2].Id == "later", "events should sort chronologically");

var tempRoot = Path.Combine(Path.GetTempPath(), "StickyNotePremium.Verification", Guid.NewGuid().ToString("N"));
try
{
    var settingsService = new SettingsService(tempRoot);
    var settings = AppSettings.CreateDefault();
    settings.Events = new List<HolidayEvent>
    {
        new() { Id = "one", Name = "Tết 2027", TargetDateTime = new DateTime(2027, 2, 6, 0, 0, 0, DateTimeKind.Local) },
        new() { Id = "two", Name = "Quốc khánh", TargetDateTime = new DateTime(2027, 9, 2, 0, 0, 0, DateTimeKind.Local) }
    };
    settings.BackgroundColor = "#123456";
    settingsService.Save(settings);
    var restored = settingsService.Load();
    Expect(restored.Events.Count == 2, "settings should restore multiple events");
    Expect(restored.Events.Any(x => x.Name == "Tết 2027"), "settings should restore event names");
    Expect(restored.BackgroundColor == "#123456", "settings should preserve appearance settings");

    var legacy = """
    {
      "EventName": "Legacy Tết",
      "TargetDateTime": "2027-02-06T00:00:00",
      "BackgroundColor": "#654321",
      "AlwaysOnTop": true
    }
    """;
    File.WriteAllText(settingsService.SettingsPath, legacy);
    var migrated = settingsService.Load();
    Expect(migrated.Events.Count == 1, "legacy single event should migrate to Events list");
    Expect(migrated.Events[0].Name == "Legacy Tết", "legacy event name should migrate");
    Expect(migrated.BackgroundColor == "#654321", "legacy appearance settings should survive migration");

    File.WriteAllText(settingsService.SettingsPath, "{ this is not valid json");
    var fallback = settingsService.Load();
    Expect(fallback.Events.Count > 0, "corrupt JSON should fall back to defaults with at least one event");
}
finally
{
    if (Directory.Exists(tempRoot))
    {
        Directory.Delete(tempRoot, true);
    }
}

if (failures.Count > 0)
{
    Console.Error.WriteLine($"FAILED: {failures.Count} verification(s)");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($" - {failure}");
    }
    Environment.Exit(1);
}

Console.WriteLine("PASS: countdown, multi-event selection, persistence and legacy migration checks");
