// BlinkNotifier.Core — pure schedule check (ARCH.md § Components, Epic 3)
using BlinkNotifier.Settings;

namespace BlinkNotifier.Core.Schedule;

public static class ScheduleGuard
{
    /// <summary>Returns true when a notification should fire at the given moment.</summary>
    public static bool ShouldFire(DateTimeOffset now, BlinkSettings settings)
    {
        if (!settings.ScheduleEnabled) return true;

        // Use .DateTime (the offset's own date/time) so tests are timezone-independent.
        // In production DateTimeOffset.Now carries the local offset, so .DateTime == local time.
        var dayOfWeek = now.DateTime.DayOfWeek;
        if (!settings.ActiveDays.Contains(dayOfWeek)) return false;

        var timeOfDay = now.DateTime.TimeOfDay;
        return timeOfDay >= settings.ScheduleStartTime
            && timeOfDay < settings.ScheduleEndTime;
    }
}
