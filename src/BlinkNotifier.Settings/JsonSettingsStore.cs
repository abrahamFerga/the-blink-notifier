// BlinkNotifier.Settings — System.Text.Json persistence (ARCH.md § Data model, ADR-0013)
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BlinkNotifier.Settings;

public sealed class JsonSettingsStore : ISettingsStore
{
    public static readonly string DefaultPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BlinkNotifier",
        "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ILogger<JsonSettingsStore> _logger;
    private readonly string _settingsPath;

    public JsonSettingsStore(ILogger<JsonSettingsStore> logger, string? settingsPath = null)
    {
        _logger = logger;
        _settingsPath = settingsPath ?? DefaultPath;
    }

    public async Task<BlinkSettings> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_settingsPath))
        {
            _logger.LogInformation("Settings file not found; returning defaults.");
            return new BlinkSettings();
        }

        try
        {
            await using var stream = File.OpenRead(_settingsPath);
            var raw = await JsonSerializer.DeserializeAsync<BlinkSettings>(stream, JsonOptions, ct);
            return Migrate(raw ?? new BlinkSettings());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load settings; returning defaults.");
            return new BlinkSettings();
        }
    }

    public async Task SaveAsync(BlinkSettings settings, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        var tmp = _settingsPath + ".tmp";

        await using (var stream = File.Create(tmp))
            await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, ct);

        // Atomic replace: rename temp file over destination (handles first-save and overwrite).
        File.Move(tmp, _settingsPath, overwrite: true);
        _logger.LogDebug("Settings saved to {Path}.", _settingsPath);
    }

    private static BlinkSettings Migrate(BlinkSettings raw) =>
        raw.SchemaVersion switch
        {
            >= 1 => raw,   // current schema — no migration needed
            _ => new BlinkSettings(),
        };
}
