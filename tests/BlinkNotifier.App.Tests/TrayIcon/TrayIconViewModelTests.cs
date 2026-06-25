// BlinkNotifier.App.Tests — TrayIconViewModel tooltip, pause state, and command behaviour
using BlinkNotifier.App.TrayIcon;
using BlinkNotifier.Core.Fullscreen;
using BlinkNotifier.Core.Timer;
using Microsoft.Extensions.Hosting;

namespace BlinkNotifier.App.Tests.TrayIcon;

public sealed class TrayIconViewModelTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static (TrayIconViewModel Vm, FullscreenState Fullscreen) Build()
    {
        var fullscreen = new FullscreenState();
        var vm = new TrayIconViewModel(
            new FakeTimer(),
            fullscreen,
            new FakeLifetime(),
            NullLogger<TrayIconViewModel>.Instance);
        return (vm, fullscreen);
    }

    // ── ToolTip ───────────────────────────────────────────────────────────────

    [Fact]
    public void ToolTip_WhenRunning_ContainsActive()
    {
        var (vm, _) = Build();
        Assert.Contains("Active", vm.ToolTip);
    }

    [Fact]
    public void ToolTip_WhenPaused_ContainsPaused()
    {
        var (vm, fs) = Build();
        fs.SetActive(true);
        Assert.Contains("PAUSED", vm.ToolTip);
    }

    [Fact]
    public void ToolTip_WhenStopped_ContainsStopped()
    {
        var (vm, _) = Build();
        vm.StopCommand.Execute(null);
        Assert.Contains("Stopped", vm.ToolTip);
    }

    // ── fullscreen state changes ───────────────────────────────────────────────

    [Fact]
    public void FullscreenActivated_SetsPausedTrue()
    {
        var (vm, fs) = Build();
        fs.SetActive(true);
        Assert.True(vm.IsPaused);
    }

    [Fact]
    public void FullscreenDeactivated_SetsPausedFalse()
    {
        var (vm, fs) = Build();
        fs.SetActive(true);
        fs.SetActive(false);
        Assert.False(vm.IsPaused);
    }

    [Fact]
    public void FullscreenActivated_FiresTrayIconStateChanged()
    {
        var (vm, fs) = Build();
        bool? fired = null;
        vm.TrayIconStateChanged += v => fired = v;

        fs.SetActive(true);

        Assert.True(fired);
    }

    [Fact]
    public void FullscreenDeactivated_FiresTrayIconStateChangedFalse()
    {
        var (vm, fs) = Build();
        fs.SetActive(true); // arm it first
        bool? fired = null;
        vm.TrayIconStateChanged += v => fired = v;

        fs.SetActive(false);

        Assert.False(fired);
    }

    // ── Start / Stop commands ─────────────────────────────────────────────────

    [Fact]
    public void StartCommand_CannotExecute_WhenAlreadyRunning()
    {
        var (vm, _) = Build();
        Assert.False(vm.StartCommand.CanExecute(null));
    }

    [Fact]
    public void StopCommand_CanExecute_WhenRunning()
    {
        var (vm, _) = Build();
        Assert.True(vm.StopCommand.CanExecute(null));
    }

    // ── Dispose ───────────────────────────────────────────────────────────────

    [Fact]
    public void Dispose_UnsubscribesFromFullscreenChanged()
    {
        var (vm, fs) = Build();
        vm.Dispose();

        // After dispose the event should not update IsPaused.
        fs.SetActive(true);
        Assert.False(vm.IsPaused);
    }

    // ── test doubles ──────────────────────────────────────────────────────────

    private sealed class FakeTimer : ITimerControl
    {
        public void Start() { }
        public void Stop() { }
        public void ResetTimer() { }
    }

    private sealed class FakeLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => default;
        public CancellationToken ApplicationStopping => default;
        public CancellationToken ApplicationStopped => default;
        public void StopApplication() { }
    }
}
