// BlinkNotifier.Core — 5-second fullscreen poll loop (ARCH.md § Components, Epic 4, ADR-0004)
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BlinkNotifier.Core.Fullscreen;

public sealed class FullscreenPoller(FullscreenState state, ILogger<FullscreenPoller> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var active = DetectFullscreen();
                state.SetActive(active);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Fullscreen detection tick failed.");
            }
        }
    }

    private static bool DetectFullscreen()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == nint.Zero) return false;

        if (!NativeMethods.GetWindowRect(hwnd, out var winRect)) return false;

        var hMonitor = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
        if (hMonitor == nint.Zero) return false;

        var mi = NativeMethods.MONITORINFO.Create();
        if (!NativeMethods.GetMonitorInfo(hMonitor, ref mi)) return false;

        var mon = mi.rcMonitor;
        return winRect.Left <= mon.Left
            && winRect.Top <= mon.Top
            && winRect.Right >= mon.Right
            && winRect.Bottom >= mon.Bottom;
    }
}
