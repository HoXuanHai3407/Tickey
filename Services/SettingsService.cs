using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using StickyNotePremium.Models;

namespace StickyNotePremium.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Regex HexColorPattern = new(
        "^#(?:[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly string _settingsDirectory;

    public SettingsService(string? baseDirectory = null)
    {
        _settingsDirectory = baseDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StickyNotePremium");

        SettingsPath = Path.Combine(_settingsDirectory, "settings.json");
    }

    public string SettingsPath { get; }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return AppSettings.CreateDefault();
            }

            var json = File.ReadAllText(SettingsPath);
            using var document = JsonDocument.Parse(json);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();

            if (!TryGetPropertyIgnoreCase(document.RootElement, "Events", out _))
            {
                MigrateLegacyEvent(settings, document.RootElement);
            }

            return Sanitize(settings);
        }
        catch
        {
            return AppSettings.CreateDefault();
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Directory.CreateDirectory(_settingsDirectory);

        var clean = Sanitize(settings);
        var json = JsonSerializer.Serialize(clean, JsonOptions);
        var temporaryPath = SettingsPath + ".tmp";

        try
        {
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, SettingsPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch
                {
                    // Best-effort cleanup only. The real settings file is already safe.
                }
            }
        }
    }

    private static void MigrateLegacyEvent(AppSettings settings, JsonElement root)
    {
        if (!TryGetPropertyIgnoreCase(root, "TargetDateTime", out var targetElement))
        {
            return;
        }

        DateTime target;
        if (targetElement.ValueKind == JsonValueKind.String)
        {
            if (!targetElement.TryGetDateTime(out target))
            {
                return;
            }
        }
        else
        {
            return;
        }

        var name = "Sự kiện đã nhập trước đây";
        if (TryGetPropertyIgnoreCase(root, "EventName", out var nameElement) && nameElement.ValueKind == JsonValueKind.String)
        {
            var legacyName = nameElement.GetString();
            if (!string.IsNullOrWhiteSpace(legacyName))
            {
                name = legacyName.Trim();
            }
        }

        settings.Events = new List<HolidayEvent>
        {
            new()
            {
                Name = name,
                TargetDateTime = NormalizeLocal(target)
            }
        };
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement root, string propertyName, out JsonElement value)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static AppSettings Sanitize(AppSettings settings)
    {
        var defaults = AppSettings.CreateDefault();
        var backgroundColor = settings.BackgroundColor;

        return new AppSettings
        {
            Events = SanitizeEvents(settings.Events),
            BackgroundColor = HexColorPattern.IsMatch(backgroundColor ?? string.Empty)
                ? backgroundColor!.ToUpperInvariant()
                : defaults.BackgroundColor,
            BackgroundImagePath = string.IsNullOrWhiteSpace(settings.BackgroundImagePath)
                ? null
                : settings.BackgroundImagePath,
            BackgroundImageOpacity = Clamp01(settings.BackgroundImageOpacity, defaults.BackgroundImageOpacity),
            OverlayOpacity = Clamp01(settings.OverlayOpacity, defaults.OverlayOpacity),
            ImageStretchMode = settings.ImageStretchMode is "UniformToFill" or "Uniform" or "Fill"
                ? settings.ImageStretchMode
                : defaults.ImageStretchMode,
            ForegroundTheme = settings.ForegroundTheme is "Auto" or "Light" or "Dark"
                ? settings.ForegroundTheme
                : defaults.ForegroundTheme,
            AlwaysOnTop = settings.AlwaysOnTop,
            WindowLeft = double.IsFinite(settings.WindowLeft) ? settings.WindowLeft : defaults.WindowLeft,
            WindowTop = double.IsFinite(settings.WindowTop) ? settings.WindowTop : defaults.WindowTop,
            WindowWidth = SanitizeFinite(settings.WindowWidth, defaults.WindowWidth, 650, 3000),
            WindowHeight = SanitizeFinite(settings.WindowHeight, defaults.WindowHeight, 210, 2000)
        };
    }

    private static List<HolidayEvent> SanitizeEvents(IEnumerable<HolidayEvent>? events)
    {
        var clean = new List<HolidayEvent>();
        var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (events is null)
        {
            return clean;
        }

        foreach (var item in events)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.Name))
            {
                continue;
            }

            var target = NormalizeLocal(item.TargetDateTime);
            if (target.Year is < 1900 or > 9998)
            {
                continue;
            }

            var id = item.Id?.Trim() ?? string.Empty;
            if (id.Length == 0 || !usedIds.Add(id))
            {
                do
                {
                    id = Guid.NewGuid().ToString("N");
                }
                while (!usedIds.Add(id));
            }

            clean.Add(new HolidayEvent
            {
                Id = id,
                Name = item.Name.Trim(),
                TargetDateTime = target
            });
        }

        return clean
            .OrderBy(item => item.TargetDateTime)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static DateTime NormalizeLocal(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value.ToLocalTime(),
            DateTimeKind.Local => value,
            _ => DateTime.SpecifyKind(value, DateTimeKind.Local)
        };
    }

    private static double Clamp01(double value, double fallback)
    {
        if (!double.IsFinite(value))
        {
            return fallback;
        }

        return Math.Clamp(value, 0.0, 1.0);
    }

    private static double SanitizeFinite(double value, double fallback, double minimum, double maximum)
    {
        if (!double.IsFinite(value))
        {
            return fallback;
        }

        return Math.Clamp(value, minimum, maximum);
    }
}
