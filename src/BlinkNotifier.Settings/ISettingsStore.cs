// BlinkNotifier.Settings — persistence interface (ARCH.md § Data model)
namespace BlinkNotifier.Settings;

public interface ISettingsStore
{
    Task<BlinkSettings> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(BlinkSettings settings, CancellationToken ct = default);
}
