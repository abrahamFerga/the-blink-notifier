// BlinkNotifier.Core — toast dispatch via CommunityToolkit (ARCH.md § Components, Epic 2, ADR-0003)
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.Notifications;

namespace BlinkNotifier.Core.Toast;

public sealed class ToastDispatcher(ILogger<ToastDispatcher> logger) : IToastDispatcher
{
    public Task ShowAsync(CancellationToken ct = default)
    {
        try
        {
            new ToastContentBuilder()
                .AddText("Time to blink!")
                .AddText("Look at something 20 feet away for 20 seconds.")
                .AddButton(new ToastButton("Snooze 5m", "action=snooze;duration=5"))
                .AddButton(new ToastButton("Snooze 15m", "action=snooze;duration=15"))
                .AddButton(new ToastButton("Snooze 60m", "action=snooze;duration=60"))
                .AddButton(new ToastButton("Dismiss", "action=dismiss"))
                .Show();

            logger.LogDebug("Toast notification shown.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to show toast notification.");
        }

        return Task.CompletedTask;
    }
}
