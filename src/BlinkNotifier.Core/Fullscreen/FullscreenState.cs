// BlinkNotifier.Core — in-memory fullscreen state (ARCH.md § Components, Epic 4)
namespace BlinkNotifier.Core.Fullscreen;

public sealed class FullscreenState
{
    private int _isActive; // 0 = false, 1 = true (Interlocked-compatible)

    public bool IsFullscreenActive => Interlocked.CompareExchange(ref _isActive, 0, 0) == 1;
    public DateTimeOffset? FullscreenEnteredAt { get; private set; }

    public event EventHandler<bool>? FullscreenChanged;

    public void SetActive(bool active)
    {
        var prev = Interlocked.Exchange(ref _isActive, active ? 1 : 0);
        if (prev == (active ? 1 : 0)) return; // no change

        FullscreenEnteredAt = active ? DateTimeOffset.UtcNow : null;
        FullscreenChanged?.Invoke(this, active);
    }
}
