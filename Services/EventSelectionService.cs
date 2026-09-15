using StickyNotePremium.Models;

namespace StickyNotePremium.Services;

public sealed class EventSelectionService
{
    public HolidayEvent? GetNearestUpcoming(IEnumerable<HolidayEvent> events, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(events);

        return events
            .Where(item => item.TargetDateTime > now)
            .OrderBy(item => item.TargetDateTime)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .FirstOrDefault();
    }

    public List<HolidayEvent> GetChronological(IEnumerable<HolidayEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        return events
            .OrderBy(item => item.TargetDateTime)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }
}
