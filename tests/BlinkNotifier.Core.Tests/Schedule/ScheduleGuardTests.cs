// Tests: ScheduleGuard pure function (ARCH.md § Components, Epic 3)
using BlinkNotifier.Core.Schedule;
using BlinkNotifier.Settings;

namespace BlinkNotifier.Core.Tests.Schedule;

public sealed class ScheduleGuardTests
{
    [Fact]
    public void ShouldFire_WhenScheduleDisabled_ReturnsTrue()
    {
        var settings = new BlinkSettings { ScheduleEnabled = false };
        var now = new DateTimeOffset(2026, 6, 24, 14, 0, 0, TimeSpan.Zero); // Tuesday 14:00 UTC
        Assert.True(ScheduleGuard.ShouldFire(now, settings));
    }

    [Fact]
    public void ShouldFire_WithinWindowOnActiveDay_ReturnsTrue()
    {
        var settings = new BlinkSettings
        {
            ScheduleEnabled   = true,
            ScheduleStartTime = TimeSpan.FromHours(9),
            ScheduleEndTime   = TimeSpan.FromHours(18),
            ActiveDays        = [DayOfWeek.Wednesday], // 2026-06-24 is a Wednesday
        };
        var now = new DateTimeOffset(2026, 6, 24, 10, 0, 0, TimeSpan.Zero);
        Assert.True(ScheduleGuard.ShouldFire(now, settings));
    }

    [Fact]
    public void ShouldFire_OutsideTimeWindow_ReturnsFalse()
    {
        var settings = new BlinkSettings
        {
            ScheduleEnabled   = true,
            ScheduleStartTime = TimeSpan.FromHours(9),
            ScheduleEndTime   = TimeSpan.FromHours(18),
            ActiveDays        = [DayOfWeek.Tuesday],
        };
        var now = new DateTimeOffset(2026, 6, 24, 20, 0, 0, TimeSpan.Zero); // 20:00 — after window
        Assert.False(ScheduleGuard.ShouldFire(now, settings));
    }

    [Fact]
    public void ShouldFire_OnInactiveDay_ReturnsFalse()
    {
        var settings = new BlinkSettings
        {
            ScheduleEnabled   = true,
            ScheduleStartTime = TimeSpan.FromHours(9),
            ScheduleEndTime   = TimeSpan.FromHours(18),
            ActiveDays        = [DayOfWeek.Monday], // only Monday
        };
        // 2026-06-24 is a Wednesday
        var now = new DateTimeOffset(2026, 6, 24, 10, 0, 0, TimeSpan.Zero);
        Assert.False(ScheduleGuard.ShouldFire(now, settings));
    }
}
