// Tests: SnoozeStateMachine thread-safe state (ARCH.md § Components, Epic 2)
using BlinkNotifier.Core.Timer;

namespace BlinkNotifier.Core.Tests.Timer;

public sealed class SnoozeStateMachineTests
{
    [Fact]
    public void Initially_IsNotSnoozed()
    {
        var sm = new SnoozeStateMachine();
        Assert.False(sm.IsSnoozed);
    }

    [Fact]
    public void Snooze_SetsIsSnoozed()
    {
        var sm = new SnoozeStateMachine();
        sm.Snooze(TimeSpan.FromMinutes(5));
        Assert.True(sm.IsSnoozed);
        Assert.True(sm.SnoozedUntil > DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Clear_RemovesSnooze()
    {
        var sm = new SnoozeStateMachine();
        sm.Snooze(TimeSpan.FromMinutes(5));
        sm.Clear();
        Assert.False(sm.IsSnoozed);
    }

    [Fact]
    public void SnoozedUntil_IsApproximatelyNowPlusDuration()
    {
        var sm = new SnoozeStateMachine();
        var duration = TimeSpan.FromMinutes(15);
        var before = DateTimeOffset.UtcNow;
        sm.Snooze(duration);
        var after = DateTimeOffset.UtcNow;

        Assert.InRange(sm.SnoozedUntil,
            before + duration - TimeSpan.FromSeconds(1),
            after  + duration + TimeSpan.FromSeconds(1));
    }
}
