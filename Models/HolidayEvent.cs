namespace StickyNotePremium.Models;

public sealed class HolidayEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public DateTime TargetDateTime { get; set; } = DateTime.Now.AddDays(1);

    public HolidayEvent Clone() => new()
    {
        Id = Id,
        Name = Name,
        TargetDateTime = TargetDateTime
    };
}
