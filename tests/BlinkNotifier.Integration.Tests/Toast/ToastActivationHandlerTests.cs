// BlinkNotifier.Integration.Tests — toast activation routing (ARCH.md § API surface, ADR-0003)
using BlinkNotifier.Core.Timer;
using BlinkNotifier.Core.Toast;

namespace BlinkNotifier.Integration.Tests.Toast;

public sealed class ToastActivationHandlerTests
{
    [Fact]
    public void Dispatch_SnoozeAction_SetsSnoozeActive()
    {
        var snooze = new SnoozeStateMachine();
        ToastActivationHandler.Dispatch("action=snooze;duration=5", snooze, null, NullLogger.Instance);
        Assert.True(snooze.IsSnoozed);
    }

    [Theory]
    [InlineData("action=snooze;duration=5", 5)]
    [InlineData("action=snooze;duration=15", 15)]
    [InlineData("action=snooze;duration=60", 60)]
    public void Dispatch_SnoozeAction_SnoozesForCorrectDuration(string rawArgs, int expectedMinutes)
    {
        var snooze = new SnoozeStateMachine();
        var before = DateTimeOffset.UtcNow;

        ToastActivationHandler.Dispatch(rawArgs, snooze, null, NullLogger.Instance);

        Assert.True(snooze.IsSnoozed);
        var remainingSeconds = (snooze.SnoozedUntil - before).TotalSeconds;
        // Allow 5-second margin for test execution time
        Assert.InRange(remainingSeconds, expectedMinutes * 60 - 5, expectedMinutes * 60 + 5);
    }

    [Fact]
    public void Dispatch_DismissAction_ClearsActiveSnooze()
    {
        var snooze = new SnoozeStateMachine();
        snooze.Snooze(TimeSpan.FromMinutes(60));
        Assert.True(snooze.IsSnoozed);

        ToastActivationHandler.Dispatch("action=dismiss", snooze, null, NullLogger.Instance);

        Assert.False(snooze.IsSnoozed);
    }

    [Fact]
    public void Dispatch_UnknownAction_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            ToastActivationHandler.Dispatch("action=unknown", null, null, NullLogger.Instance));
        Assert.Null(ex);
    }

    [Fact]
    public void Dispatch_MissingActionKey_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            ToastActivationHandler.Dispatch("foo=bar", null, null, NullLogger.Instance));
        Assert.Null(ex);
    }

    [Fact]
    public void Dispatch_AmpersandSeparator_DoesNotRoute()
    {
        // Regression: '&' is NOT a valid ToastArguments separator (';' is required).
        // If someone accidentally uses '&' in the button arguments, the action key
        // value would be "snooze&duration=5" instead of "snooze", so the snooze
        // switch case would not fire and IsSnoozed stays false.
        var snooze = new SnoozeStateMachine();
        ToastActivationHandler.Dispatch("action=snooze&duration=5", snooze, null, NullLogger.Instance);
        Assert.False(snooze.IsSnoozed);
    }

    [Fact]
    public void Dispatch_SnoozeWithMissingDuration_DoesNotSnooze()
    {
        // The 'when' clause on the snooze case requires a parseable duration key.
        // If the key is absent the case falls through to 'default' and is a no-op.
        var snooze = new SnoozeStateMachine();
        ToastActivationHandler.Dispatch("action=snooze", snooze, null, NullLogger.Instance);
        Assert.False(snooze.IsSnoozed);
    }
}
