// Tests: BlinkSettingsValidator (ARCH.md § Cross-cutting wiring, ADR-0007)
using BlinkNotifier.Settings;
using Microsoft.Extensions.Options;

namespace BlinkNotifier.Settings.Tests;

public sealed class BlinkSettingsValidatorTests
{
    private readonly BlinkSettingsValidator _sut = new();

    [Fact]
    public void Defaults_PassValidation()
    {
        var result = _sut.Validate(null, new BlinkSettings());
        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(61)]
    public void InvalidInterval_FailsValidation(int interval)
    {
        var s = new BlinkSettings { ReminderIntervalMinutes = interval };
        var result = _sut.Validate(null, s);
        Assert.NotEqual(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void StartTimeAfterEndTime_WhenScheduleEnabled_FailsValidation()
    {
        var s = new BlinkSettings
        {
            ScheduleEnabled   = true,
            ScheduleStartTime = TimeSpan.FromHours(18),
            ScheduleEndTime   = TimeSpan.FromHours(9),
        };
        var result = _sut.Validate(null, s);
        Assert.NotEqual(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void EmptySnoozeOptions_FailsValidation()
    {
        var s = new BlinkSettings { SnoozeOptionsMinutes = [] };
        var result = _sut.Validate(null, s);
        Assert.NotEqual(ValidateOptionsResult.Success, result);
    }
}
