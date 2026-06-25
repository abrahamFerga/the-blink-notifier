// BlinkNotifier.Core — COM activation callback for toast actions (ARCH.md § API surface, ADR-0003)
using BlinkNotifier.Core.Timer;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.Notifications;

namespace BlinkNotifier.Core.Toast;

public static class ToastActivationHandler
{
    private static SnoozeStateMachine? _snooze;
    private static ReminderTimerService? _timer;
    private static ILogger? _logger;

    public static void Register(
        SnoozeStateMachine snooze,
        ReminderTimerService timer,
        ILogger logger)
    {
        _snooze = snooze;
        _timer = timer;
        _logger = logger;

        // Wire the static OnActivated callback once per process lifetime.
        ToastNotificationManagerCompat.OnActivated += OnActivated;
    }

    private static void OnActivated(ToastNotificationActivatedEventArgsCompat e)
        => Dispatch(e.Argument, _snooze, _timer, _logger);

    // internal for BlinkNotifier.Integration.Tests
    internal static void Dispatch(
        string rawArgs,
        SnoozeStateMachine? snooze,
        ReminderTimerService? timer,
        ILogger? logger)
    {
        var args = ToastArguments.Parse(rawArgs);

        if (!args.TryGetValue("action", out var action)) return;

        switch (action)
        {
            case "snooze" when args.TryGetValue("duration", out var durStr)
                            && int.TryParse(durStr, out var minutes):
                snooze?.Snooze(TimeSpan.FromMinutes(minutes));
                timer?.ResetTimer(); // cancel current wait; loop will see snooze and wait N min
                logger?.LogInformation("Snoozed for {Minutes} minutes.", minutes);
                break;

            case "dismiss":
                snooze?.Clear();
                timer?.ResetTimer(); // restart full interval from now (next fire = now + interval)
                logger?.LogInformation("Toast dismissed — timer reset.");
                break;

            default:
                logger?.LogWarning("Unknown toast action: {Action}.", action);
                break;
        }
    }
}
