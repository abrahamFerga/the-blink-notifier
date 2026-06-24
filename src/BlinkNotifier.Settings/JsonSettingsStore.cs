// BlinkNotifier.Settings — System.Text.Json persistence (ARCH.md § Data model, ADR-0013)
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BlinkNotifier.Settings;

public sealed class JsonSettingsStore(ILogger<JsonSettingsStore> logger) : ISettingsStore
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BlinkNotifier",
        "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public async Task<BlinkSettings> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(SettingsPath))
        {
            logger.LogInformation("Settings file not found; returning defaults.");
            return new BlinkSettings();
        }

        try
        {
            await using var stream = File.OpenRead(SettingsPath);
            var raw = await JsonSerializer.DeserializeAsync<BlinkSettings>(stream, JsonOptions, ct);
            return Migrate(raw ?? new BlinkSettings());
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load settings; returning defaults.");
            return new BlinkSettings();
        }
    }

    public async Task SaveAsync(BlinkSettings settings, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        var tmp = SettingsPath + ".tmp";

        await using (var stream = File.Create(tmp))
            await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, ct);

        // Atomic replace: temp file rename prevents corruption on crash mid-write.
        File.Replace(tmp, SettingsPath, null);
        logger.LogDebug("Settings saved to {Path}.", SettingsPath);
    }

    private static BlinkSettings Migrate(BlinkSettings raw) =>
        raw.SchemaVersion switch
        {
            >= 1 => raw,   // current schema — no migration needed
            _ => new BlinkSettings(),
        };
}
