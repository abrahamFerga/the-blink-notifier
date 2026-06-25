// BlinkNotifier.Core.Tests — FullscreenState state machine (ARCH.md § Components, Epic 4)
using BlinkNotifier.Core.Fullscreen;

namespace BlinkNotifier.Core.Tests.Fullscreen;

public sealed class FullscreenStateTests
{
    [Fact]
    public void Initially_NotActive()
    {
        var state = new FullscreenState();
        Assert.False(state.IsFullscreenActive);
        Assert.Null(state.FullscreenEnteredAt);
    }

    [Fact]
    public void SetActive_True_SetsIsActiveAndTimestamp()
    {
        var state = new FullscreenState();
        var before = DateTimeOffset.UtcNow;
        state.SetActive(true);
        var after = DateTimeOffset.UtcNow;

        Assert.True(state.IsFullscreenActive);
        Assert.NotNull(state.FullscreenEnteredAt);
        Assert.InRange(state.FullscreenEnteredAt!.Value, before, after);
    }

    [Fact]
    public void SetActive_False_ClearsActiveAndTimestamp()
    {
        var state = new FullscreenState();
        state.SetActive(true);
        state.SetActive(false);

        Assert.False(state.IsFullscreenActive);
        Assert.Null(state.FullscreenEnteredAt);
    }

    [Fact]
    public void SetActive_FiresChangedEvent_OnTransition()
    {
        var state = new FullscreenState();
        var events = new List<bool>();
        state.FullscreenChanged += (_, active) => events.Add(active);

        state.SetActive(true);
        state.SetActive(false);

        Assert.Equal([true, false], events);
    }

    [Fact]
    public void SetActive_DoesNotFireEvent_WhenValueUnchanged()
    {
        var state = new FullscreenState();
        int eventCount = 0;
        state.FullscreenChanged += (_, _) => eventCount++;

        state.SetActive(true);
        state.SetActive(true); // same value — no transition
        state.SetActive(true);

        Assert.Equal(1, eventCount);
    }

    [Fact]
    public void SetActive_False_WhenAlreadyFalse_DoesNotFireEvent()
    {
        var state = new FullscreenState();
        int eventCount = 0;
        state.FullscreenChanged += (_, _) => eventCount++;

        state.SetActive(false); // already false by default

        Assert.Equal(0, eventCount);
    }
}
