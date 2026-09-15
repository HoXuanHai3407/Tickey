using StickyNotePremium.Models;

namespace StickyNotePremium.Services;

public sealed class CountdownCalculator
{
    private const double AverageDaysPerMonth = 30.44;

    public CountdownSnapshot Calculate(DateTime now, DateTime target)
    {
        if (target <= now)
        {
            return new CountdownSnapshot(0, 0, 0, 0, 0, 0, 0, true);
        }

        var remaining = target - now;
        var wholeSeconds = Math.Max(0L, (long)Math.Floor(remaining.TotalSeconds));

        var daysLong = wholeSeconds / 86_400L;
        var hours = (int)((wholeSeconds % 86_400L) / 3_600L);
        var minutes = (int)((wholeSeconds % 3_600L) / 60L);
        var seconds = (int)(wholeSeconds % 60L);

        var days = daysLong > int.MaxValue ? int.MaxValue : (int)daysLong;
        var calendarDaysDouble = Math.Ceiling(remaining.TotalDays);
        var calendarDays = calendarDaysDouble > int.MaxValue ? int.MaxValue : (int)calendarDaysDouble;

        var approxMonths = (int)Math.Floor(days / AverageDaysPerMonth);
        var approxDays = Math.Max(0, (int)Math.Floor(days - (approxMonths * AverageDaysPerMonth)));

        return new CountdownSnapshot(
            days,
            hours,
            minutes,
            seconds,
            calendarDays,
            approxMonths,
            approxDays,
            false);
    }
}
