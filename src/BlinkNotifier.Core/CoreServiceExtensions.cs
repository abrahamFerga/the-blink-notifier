// BlinkNotifier.Core — DI registration (ARCH.md § Solution layout)
using BlinkNotifier.Core.Fullscreen;
using BlinkNotifier.Core.Timer;
using BlinkNotifier.Core.Toast;
using Microsoft.Extensions.DependencyInjection;

namespace BlinkNotifier.Core;

public static class CoreServiceExtensions
{
    public static IServiceCollection AddBlinkCore(this IServiceCollection services)
    {
        services.AddSingleton<SnoozeStateMachine>();
        services.AddSingleton<FullscreenState>();
        services.AddSingleton<ToastDispatcher>();
        services.AddSingleton<IToastDispatcher>(sp => sp.GetRequiredService<ToastDispatcher>());
        services.AddSingleton<ReminderTimerService>();
        services.AddSingleton<ITimerControl>(sp => sp.GetRequiredService<ReminderTimerService>());
        services.AddHostedService(sp => sp.GetRequiredService<ReminderTimerService>());
        services.AddHostedService<FullscreenPoller>();
        return services;
    }
}
