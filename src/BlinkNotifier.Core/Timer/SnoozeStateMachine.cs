// BlinkNotifier.Core — thread-safe snooze state (ARCH.md § Components, Epic 2)
namespace BlinkNotifier.Core.Timer;

public sealed class SnoozeStateMachine
{
    // Store as UTC ticks; 0 means "not snoozed".
    private long _snoozedUntilTicks;

    public bool IsSnoozed => DateTimeOffset.UtcNow.Ticks < Interlocked.Read(ref _snoozedUntilTicks);

    public DateTimeOffset SnoozedUntil =>
        new(Interlocked.Read(ref _snoozedUntilTicks), TimeSpan.Zero);

    public void Snooze(TimeSpan duration)
    {
        var until = (DateTimeOffset.UtcNow + duration).Ticks;
        Interlocked.Exchange(ref _snoozedUntilTicks, until);
    }

    public void Clear() => Interlocked.Exchange(ref _snoozedUntilTicks, 0L);
}
