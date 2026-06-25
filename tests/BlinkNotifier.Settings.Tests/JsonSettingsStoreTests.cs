// Tests: JsonSettingsStore JSON round-trip and defaults (ARCH.md § Data model, ADR-0013)
using BlinkNotifier.Settings;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlinkNotifier.Settings.Tests;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly string _settingsPath;
    private readonly JsonSettingsStore _sut;

    public JsonSettingsStoreTests()
    {
        Directory.CreateDirectory(_tempDir);
        _settingsPath = Path.Combine(_tempDir, "settings.json");
        _sut = new JsonSettingsStore(NullLogger<JsonSettingsStore>.Instance, _settingsPath);
    }

    [Fact]
    public async Task LoadAsync_WhenFileAbsent_ReturnsDefaults()
    {
        var settings = await _sut.LoadAsync();

        Assert.Equal(20, settings.ReminderIntervalMinutes);
        Assert.False(settings.ScheduleEnabled);
        Assert.True(settings.AutoLaunchEnabled);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrips_AllFields()
    {
        var original = new BlinkSettings
        {
            ReminderIntervalMinutes = 10,
            ScheduleEnabled = true,
            ScheduleStartTime = TimeSpan.FromHours(8),
            ScheduleEndTime = TimeSpan.FromHours(17),
            ActiveDays = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday],
            AutoLaunchEnabled = false,
            SnoozeOptionsMinutes = [5, 30],
        };

        await _sut.SaveAsync(original);
        var loaded = await _sut.LoadAsync();

        Assert.Equal(original.ReminderIntervalMinutes, loaded.ReminderIntervalMinutes);
        Assert.Equal(original.ScheduleEnabled, loaded.ScheduleEnabled);
        Assert.Equal(original.ScheduleStartTime, loaded.ScheduleStartTime);
        Assert.Equal(original.ScheduleEndTime, loaded.ScheduleEndTime);
        Assert.Equal(original.ActiveDays, loaded.ActiveDays);
        Assert.Equal(original.AutoLaunchEnabled, loaded.AutoLaunchEnabled);
        Assert.Equal(original.SnoozeOptionsMinutes, loaded.SnoozeOptionsMinutes);
    }

    [Fact]
    public async Task LoadAsync_WithCorruptFile_ReturnsDefaults()
    {
        await File.WriteAllTextAsync(_settingsPath, "{ this is not valid json !");

        var settings = await _sut.LoadAsync();

        Assert.Equal(20, settings.ReminderIntervalMinutes); // default
    }

    [Fact]
    public async Task SaveAsync_IsAtomic_FileExistsAfterSave()
    {
        await _sut.SaveAsync(new BlinkSettings());
        Assert.True(File.Exists(_settingsPath));
        Assert.False(File.Exists(_settingsPath + ".tmp")); // temp file cleaned up
    }

    [Fact]
    public async Task SaveAsync_WhenCancelled_CleansUpTempFile()
    {
        // A pre-cancelled token causes SerializeAsync to throw after File.Create(tmp)
        // already ran; the catch block must delete the partial .tmp before rethrowing.
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _sut.SaveAsync(new BlinkSettings(), cts.Token));

        Assert.False(File.Exists(_settingsPath + ".tmp"));
    }

    [Fact]
    public async Task LoadAsync_WithUnknownSchemaVersion_ReturnsDefaults()
    {
        // SchemaVersion 0 is a hypothetical pre-v1 format; the Migrate() switch
        // falls through to the default case and returns a fresh BlinkSettings.
        await File.WriteAllTextAsync(_settingsPath,
            """{"SchemaVersion":0,"ReminderIntervalMinutes":5}""");

        var settings = await _sut.LoadAsync();

        Assert.Equal(20, settings.ReminderIntervalMinutes); // default, not 5
        Assert.Equal(1, settings.SchemaVersion);            // current schema
    }

    [Fact]
    public void DefaultSettings_HaveSchemaVersion1()
    {
        Assert.Equal(1, new BlinkSettings().SchemaVersion);
    }

    [Fact]
    public void DefaultSettings_HaveCorrectSnoozeOptions()
    {
        Assert.Equal([5, 15, 60], new BlinkSettings().SnoozeOptionsMinutes);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
