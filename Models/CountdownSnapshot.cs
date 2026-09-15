namespace StickyNotePremium.Models;

public readonly record struct CountdownSnapshot(
    int Days,
    int Hours,
    int Minutes,
    int Seconds,
    int CalendarDays,
    int ApproxMonths,
    int ApproxDays,
    bool HasReached);
