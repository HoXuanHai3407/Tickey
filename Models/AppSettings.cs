namespace StickyNotePremium.Models;

public sealed class AppSettings
{
    public List<HolidayEvent> Events { get; set; } = new();

    public string BackgroundColor { get; set; } = "#C92533";

    public string? BackgroundImagePath { get; set; }

    public double BackgroundImageOpacity { get; set; } = 1.0;

    public double OverlayOpacity { get; set; } = 0.05;

    public string ImageStretchMode { get; set; } = "UniformToFill";

    public string ForegroundTheme { get; set; } = "Light";

    public bool AlwaysOnTop { get; set; } = true;

    public double WindowLeft { get; set; } = 120;

    public double WindowTop { get; set; } = 120;

    public double WindowWidth { get; set; } = 790;

    public double WindowHeight { get; set; } = 240;

    public static AppSettings CreateDefault() => new()
    {
        Events = new List<HolidayEvent>
        {
            new()
            {
                Name = "Tết Nguyên Đán 2027",
                TargetDateTime = new DateTime(2027, 2, 6, 0, 0, 0, DateTimeKind.Local)
            },
            new()
            {
                Name = "Giải phóng miền Nam 2027",
                TargetDateTime = new DateTime(2027, 4, 30, 0, 0, 0, DateTimeKind.Local)
            },
            new()
            {
                Name = "Quốc Khánh 2027",
                TargetDateTime = new DateTime(2027, 9, 2, 0, 0, 0, DateTimeKind.Local)
            }
        }
    };
}
