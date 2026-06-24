// BlinkNotifier.Core.Tests — behavioral tests for ReminderTimerService (ARCH.md § Components, Epic 2)
using BlinkNotifier.Core.Fullscreen;
using BlinkNotifier.Core.Timer;
using BlinkNotifier.Core.Toast;
using BlinkNotifier.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace BlinkNotifier.Core.Tests.Timer;

public sealed class ReminderTimerServiceTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private sealed record Harness(
        ReminderTimerService Sut,
        SnoozeStateMachine Snooze,
        CountingDispatcher Dispatcher,
        FakeTimeProvider Clock);

    /// <summary>
    /// Build a test harness with a fake clock. Schedule is disabled so
    /// ScheduleGuard never suppresses the reminder in tests.
    /// </summary>
    private static Harness Build(int intervalMinutes = 20)
    {
        var clock = new FakeTimeProvider();
        var dispatcher = new CountingDispatcher();
        var snooze = new SnoozeStateMachine(clock);
        var store = new StubSettingsStore(new BlinkSettings
        {
            ReminderIntervalMinutes = intervalMinutes,
            ScheduleEnabled = false,
        });
        var sut = new ReminderTimerService(
            snooze,
            new FullscreenState(),
            store,
            dispatcher,
            NullLogger<ReminderTimerService>.Instance,
            clock);
        return new Harness(sut, snooze, dispatcher, clock);
    }

    // Advance fake time then yield to the thread pool so async continuations run.
    private static async Task AdvanceAndYield(FakeTimeProvider clock, TimeSpan delta)
    {
        clock.Advance(delta);
        await Task.Delay(50); // real delay to let timer continuations schedule and run
    }

    // ── tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task FiresOnceAfterInterval()
    {
        var h = Build(intervalMinutes: 20);
        using var cts = new CancellationTokenSource();
        await h.Sut.StartAsync(cts.Token);
        await Task.Delay(50); // let ExecuteAsync register its timer

        await AdvanceAndYield(h.Clock, TimeSpan.FromMinutes(20));

        Assert.Equal(1, h.Dispatcher.Count);

        await cts.CancelAsync();
        await h.Sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DoesNotFireBeforeIntervalElapses()
    {
        var h = Build(intervalMinutes: 20);
        using var cts = new CancellationTokenSource();
        await h.Sut.StartAsync(cts.Token);
        await Task.Delay(50);

        await AdvanceAndYield(h.Clock, TimeSpan.FromMinutes(19));

        Assert.Equal(0, h.Dispatcher.Count);

        await cts.CancelAsync();
        await h.Sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ResetTimerRestartsFullCountdown()
    {
        var h = Build(intervalMinutes: 20);
        using var cts = new CancellationTokenSource();
        await h.Sut.StartAsync(cts.Token);
        await Task.Delay(50);

        // Advance 15 minutes then reset — countdown should restart from zero
        await AdvanceAndYield(h.Clock, TimeSpan.FromMinutes(15));
        h.Sut.ResetTimer();
        await Task.Delay(50); // let loop re-register its timer after the reset

        // 19 more minutes since reset (34 total) — should NOT have fired yet
        await AdvanceAndYield(h.Clock, TimeSpan.FromMinutes(19));
        Assert.Equal(0, h.Dispatcher.Count);

        // 1 more minute (20 since reset) — should fire now
        await AdvanceAndYield(h.Clock, TimeSpan.FromMinutes(1));
        Assert.Equal(1, h.Dispatcher.Count);

        await cts.CancelAsync();
        await h.Sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SnoozeFiresWhenSnoozeExpires()
    {
        var h = Build(intervalMinutes: 20);
        // Snooze before starting so the first loop iteration enters snooze-wait
        h.Snooze.Snooze(TimeSpan.FromMinutes(5));

        using var cts = new CancellationTokenSource();
        await h.Sut.StartAsync(cts.Token);
        await Task.Delay(50); // let ExecuteAsync register the snooze timer

        // 4 minutes into snooze — should not fire
        await AdvanceAndYield(h.Clock, TimeSpan.FromMinutes(4));
        Assert.Equal(0, h.Dispatcher.Count);

        // 1 more minute — snooze expires, should fire
        await AdvanceAndYield(h.Clock, TimeSpan.FromMinutes(1));
        Assert.Equal(1, h.Dispatcher.Count);

        await cts.CancelAsync();
        await h.Sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StoppedTimer_DoesNotFire()
    {
        var h = Build(intervalMinutes: 1);
        using var cts = new CancellationTokenSource();
        await h.Sut.StartAsync(cts.Token);
        await Task.Delay(50);

        h.Sut.Stop();
        await Task.Delay(50);

        await AdvanceAndYield(h.Clock, TimeSpan.FromMinutes(2));
        Assert.Equal(0, h.Dispatcher.Count);

        await cts.CancelAsync();
        await h.Sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Restart_AfterStop_FiresAgain()
    {
        var h = Build(intervalMinutes: 1);
        using var cts = new CancellationTokenSource();
        await h.Sut.StartAsync(cts.Token);
        await Task.Delay(50);

        h.Sut.Stop();
        await Task.Delay(50);

        h.Sut.Start();
        await Task.Delay(50); // let loop re-register timer after Start resets it

        await AdvanceAndYield(h.Clock, TimeSpan.FromMinutes(1));
        Assert.Equal(1, h.Dispatcher.Count);

        await cts.CancelAsync();
        await h.Sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FullscreenActive_SuppressesFiring()
    {
        var clock = new FakeTimeProvider();
        var dispatcher = new CountingDispatcher();
        var snooze = new SnoozeStateMachine(clock);
        var fullscreen = new FullscreenState();
        fullscreen.SetActive(true);
        var store = new StubSettingsStore(new BlinkSettings { ReminderIntervalMinutes = 1, ScheduleEnabled = false });
        var sut = new ReminderTimerService(snooze, fullscreen, store, dispatcher,
            NullLogger<ReminderTimerService>.Instance, clock);

        using var cts = new CancellationTokenSource();
        await sut.StartAsync(cts.Token);
        await Task.Delay(50);

        await AdvanceAndYield(clock, TimeSpan.FromMinutes(1));

        Assert.Equal(0, dispatcher.Count);

        await cts.CancelAsync();
        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ScheduleWindow_InactiveDay_SuppressesFiring()
    {
        // ActiveDays = [] means ScheduleGuard.ShouldFire always returns false,
        // regardless of timezone — so this test is portable across CI regions.
        var clock = new FakeTimeProvider();
        var dispatcher = new CountingDispatcher();
        var store = new StubSettingsStore(new BlinkSettings
        {
            ReminderIntervalMinutes = 1,
            ScheduleEnabled = true,
            ScheduleStartTime = TimeSpan.FromHours(9),
            ScheduleEndTime = TimeSpan.FromHours(18),
            ActiveDays = [],
        });
        var sut = new ReminderTimerService(
            new SnoozeStateMachine(clock),
            new FullscreenState(),
            store,
            dispatcher,
            NullLogger<ReminderTimerService>.Instance,
            clock);

        using var cts = new CancellationTokenSource();
        await sut.StartAsync(cts.Token);
        await Task.Delay(50);

        await AdvanceAndYield(clock, TimeSpan.FromMinutes(1));

        Assert.Equal(0, dispatcher.Count);

        await cts.CancelAsync();
        await sut.StopAsync(CancellationToken.None);
    }

    // ── test doubles ─────────────────────────────────────────────────────────

    private sealed class CountingDispatcher : IToastDispatcher
    {
        private int _count;
        public int Count => _count;
        public Task ShowAsync(CancellationToken ct = default)
        {
            Interlocked.Increment(ref _count);
            return Task.CompletedTask;
        }
    }

    private sealed class StubSettingsStore(BlinkSettings settings) : ISettingsStore
    {
        public Task<BlinkSettings> LoadAsync(CancellationToken ct = default) =>
            Task.FromResult(settings);
        public Task SaveAsync(BlinkSettings s, CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}
