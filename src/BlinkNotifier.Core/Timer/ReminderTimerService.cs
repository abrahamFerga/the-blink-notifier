// BlinkNotifier.Core — interval timer hosted service (ARCH.md § Components, Epic 2)
using BlinkNotifier.Core.Fullscreen;
using BlinkNotifier.Core.Schedule;
using BlinkNotifier.Core.Toast;
using BlinkNotifier.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BlinkNotifier.Core.Timer;

public sealed class ReminderTimerService : BackgroundService
{
    private readonly SnoozeStateMachine _snooze;
    private readonly FullscreenState _fullscreen;
    private readonly ISettingsStore _settingsStore;
    private readonly IToastDispatcher _toastDispatcher;
    private readonly ILogger<ReminderTimerService> _logger;
    private readonly TimeProvider _timeProvider;

    // Replacing this CTS cancels the current Task.Delay, causing the loop to restart from now.
    private CancellationTokenSource _timerReset = new();
    private volatile bool _running = true;

    public ReminderTimerService(
        SnoozeStateMachine snooze,
        FullscreenState fullscreen,
        ISettingsStore settingsStore,
        IToastDispatcher toastDispatcher,
        ILogger<ReminderTimerService> logger,
        TimeProvider? timeProvider = null)
    {
        _snooze = snooze;
        _fullscreen = fullscreen;
        _settingsStore = settingsStore;
        _toastDispatcher = toastDispatcher;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public void Stop() => _running = false;

    public void Start()
    {
        _running = true;
        ResetTimer(); // restart countdown from now
    }

    // Cancel the current wait so the outer loop re-evaluates from the top immediately.
    public void ResetTimer()
    {
        var old = Interlocked.Exchange(ref _timerReset, new CancellationTokenSource());
        old.Cancel();
    }

    public override void Dispose()
    {
        _timerReset.Dispose();
        base.Dispose();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReminderTimerService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_running)
            {
                // Stopped by user — poll until started again.
                try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); } catch { }
                continue;
            }

            var settings = await _settingsStore.LoadAsync(stoppingToken);

            // If snoozed, wait for the snooze to expire, then fire.
            // If not snoozed, wait the full configured interval.
            TimeSpan waitFor;
            if (_snooze.IsSnoozed)
            {
                waitFor = _snooze.SnoozedUntil - _timeProvider.GetUtcNow();
                if (waitFor <= TimeSpan.Zero)
                {
                    _snooze.Clear();
                    continue;
                }
                _logger.LogDebug("Snooze active — firing in {Remaining:mm\\:ss}.", waitFor);
            }
            else
            {
                waitFor = TimeSpan.FromMinutes(settings.ReminderIntervalMinutes);
                _logger.LogDebug("Next reminder in {Interval} minutes.", settings.ReminderIntervalMinutes);
            }

            // Snapshot the current reset token before waiting.
            // ResetTimer() replaces _timerReset and cancels the old one, which unblocks this delay.
            var resetToken = _timerReset.Token;
            using var combined = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, resetToken);
            try
            {
                await Task.Delay(waitFor, _timeProvider, combined.Token);
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug("Timer reset — restarting countdown.");
                continue;
            }
            if (stoppingToken.IsCancellationRequested) break;

            // Recheck gates after the wait completes.
            if (!_running) continue;

            _snooze.Clear();

            if (_fullscreen.IsFullscreenActive)
            {
                _logger.LogDebug("Fullscreen active — suppressing; retrying in 5s.");
                try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); } catch { }
                continue;
            }

            if (!ScheduleGuard.ShouldFire(_timeProvider.GetLocalNow(), settings))
            {
                _logger.LogDebug("Outside schedule window — retrying in 60s.");
                try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); } catch { }
                continue;
            }

            await _toastDispatcher.ShowAsync(stoppingToken);
            // Loop restarts from top — next interval begins from now.
        }

        _logger.LogInformation("ReminderTimerService stopped.");
    }
}
