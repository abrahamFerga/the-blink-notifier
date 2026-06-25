// Tests: SnoozeStateMachine thread-safe state (ARCH.md § Components, Epic 2)
using BlinkNotifier.Core.Timer;
using Microsoft.Extensions.Time.Testing;

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
            after + duration + TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void IsSnoozed_ReturnsFalse_AfterSnoozeDurationElapses()
    {
        var clock = new FakeTimeProvider();
        var sm = new SnoozeStateMachine(clock);

        sm.Snooze(TimeSpan.FromMinutes(5));
        Assert.True(sm.IsSnoozed);

        clock.Advance(TimeSpan.FromMinutes(5) + TimeSpan.FromMilliseconds(1));

        Assert.False(sm.IsSnoozed);
    }

    [Fact]
    public void Clear_WhenNotSnoozed_IsIdempotent()
    {
        var sm = new SnoozeStateMachine();
        sm.Clear(); // should not throw
        Assert.False(sm.IsSnoozed);
    }

    [Fact]
    public void IsSnoozed_ReturnsFalse_ExactlyAtDeadline()
    {
        // IsSnoozed uses strict < so at exactly SnoozedUntil the snooze is considered expired.
        var clock = new FakeTimeProvider();
        var sm = new SnoozeStateMachine(clock);
        sm.Snooze(TimeSpan.FromMinutes(5));

        clock.Advance(TimeSpan.FromMinutes(5)); // exactly at the deadline, not past it

        Assert.False(sm.IsSnoozed);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(15)]
    [InlineData(60)]
    public void Snooze_SnoozedUntil_IsExactlyNowPlusDuration(int minutes)
    {
        var clock = new FakeTimeProvider();
        var sm = new SnoozeStateMachine(clock);
        var duration = TimeSpan.FromMinutes(minutes);

        sm.Snooze(duration);

        Assert.Equal(clock.GetUtcNow() + duration, sm.SnoozedUntil);
    }

    [Fact]
    public void Snooze_WhileAlreadySnoozed_ReplacesExistingSnooze()
    {
        var clock = new FakeTimeProvider();
        var sm = new SnoozeStateMachine(clock);

        sm.Snooze(TimeSpan.FromMinutes(60)); // first snooze: 60 min
        clock.Advance(TimeSpan.FromMinutes(1));
        sm.Snooze(TimeSpan.FromMinutes(5));  // second snooze: replaces with 5 min

        Assert.True(sm.IsSnoozed);
        Assert.Equal(clock.GetUtcNow() + TimeSpan.FromMinutes(5), sm.SnoozedUntil);
    }
}
