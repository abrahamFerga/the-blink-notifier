// BlinkNotifier.Core — thread-safe snooze state (ARCH.md § Components, Epic 2)
namespace BlinkNotifier.Core.Timer;

public sealed class SnoozeStateMachine(TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private long _snoozedUntilTicks;

    public bool IsSnoozed => _timeProvider.GetUtcNow().Ticks < Interlocked.Read(ref _snoozedUntilTicks);

    public DateTimeOffset SnoozedUntil =>
        new(Interlocked.Read(ref _snoozedUntilTicks), TimeSpan.Zero);

    public void Snooze(TimeSpan duration)
    {
        var until = (_timeProvider.GetUtcNow() + duration).Ticks;
        Interlocked.Exchange(ref _snoozedUntilTicks, until);
    }

    public void Clear() => Interlocked.Exchange(ref _snoozedUntilTicks, 0L);
}
