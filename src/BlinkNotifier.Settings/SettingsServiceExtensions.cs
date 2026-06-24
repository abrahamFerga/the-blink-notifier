// BlinkNotifier.Settings — DI registration (ARCH.md § Solution layout)
using Microsoft.Extensions.DependencyInjection;

namespace BlinkNotifier.Settings;

public static class SettingsServiceExtensions
{
    public static IServiceCollection AddBlinkSettings(this IServiceCollection services)
    {
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();
        return services;
    }
}
