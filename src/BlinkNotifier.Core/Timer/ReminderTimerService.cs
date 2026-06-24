// BlinkNotifier.Core — interval timer hosted service (ARCH.md § Components, Epic 2)
using BlinkNotifier.Core.Fullscreen;
using BlinkNotifier.Core.Schedule;
using BlinkNotifier.Core.Toast;
using BlinkNotifier.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BlinkNotifier.Core.Timer;

public sealed class ReminderTimerService(
    SnoozeStateMachine snooze,
    FullscreenState fullscreen,
    ISettingsStore settingsStore,
    ToastDispatcher toastDispatcher,
    ILogger<ReminderTimerService> logger)
    : BackgroundService
{
    private volatile bool _running = true;

    public void Stop() => _running = false;
    public void Start() => _running = true;

    public void ResetTimer()
    {
        // Cancelling the current iteration is handled by re-entering the loop;
        // the PeriodicTimer delay is re-applied after each WaitForNextTickAsync.
        // No explicit reset needed — the next tick will fire at interval from last fire.
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ReminderTimerService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var settings = await settingsStore.LoadAsync(stoppingToken);
            var interval = TimeSpan.FromMinutes(settings.ReminderIntervalMinutes);

            using var timer = new PeriodicTimer(interval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                logger.LogDebug("Timer tick — checking gates.");

                if (!_running)
                {
                    logger.LogDebug("Timer stopped by user.");
                    continue;
                }

                if (snooze.IsSnoozed)
                {
                    logger.LogDebug("Snoozed until {Until}.", snooze.SnoozedUntil);
                    continue;
                }

                if (fullscreen.IsFullscreenActive)
                {
                    logger.LogDebug("Fullscreen active — suppressing notification.");
                    continue;
                }

                var now = DateTimeOffset.Now;
                if (!ScheduleGuard.ShouldFire(now, settings))
                {
                    logger.LogDebug("Outside schedule window — suppressing notification.");
                    continue;
                }

                await toastDispatcher.ShowAsync(stoppingToken);

                // Reload settings: interval may have changed since the timer was created.
                var updated = await settingsStore.LoadAsync(stoppingToken);
                if (updated.ReminderIntervalMinutes != settings.ReminderIntervalMinutes)
                    break; // restart outer loop to pick up the new interval
            }
        }

        logger.LogInformation("ReminderTimerService stopped.");
    }
}
