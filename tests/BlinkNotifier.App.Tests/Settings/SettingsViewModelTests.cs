// BlinkNotifier.App.Tests — SettingsViewModel validation and load behaviour
using BlinkNotifier.App.Settings;
using BlinkNotifier.App.Startup;
using BlinkNotifier.Core.Timer;
using BlinkNotifier.Settings;

namespace BlinkNotifier.App.Tests.Settings;

public sealed class SettingsViewModelTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static SettingsViewModel Build(BlinkSettings? initial = null)
    {
        var store = new FakeSettingsStore(initial ?? new BlinkSettings());
        return new SettingsViewModel(
            store,
            new FakeStartupRegistrar(),
            new FakeTimer(),
            NullLogger<SettingsViewModel>.Instance);
    }

    // ── interval validation ───────────────────────────────────────────────────

    [Fact]
    public void Validate_WhenIntervalBelowMin_SetsError()
    {
        var vm = Build();
        vm.IntervalMinutes = 0;
        Assert.NotNull(vm.ValidationError);
    }

    [Fact]
    public void Validate_WhenIntervalAboveMax_SetsError()
    {
        var vm = Build();
        vm.IntervalMinutes = 61;
        Assert.NotNull(vm.ValidationError);
    }

    [Fact]
    public void Validate_WhenIntervalAtMin_ClearsError()
    {
        var vm = Build();
        vm.IntervalMinutes = 0;   // set bad first
        vm.IntervalMinutes = 1;
        Assert.Null(vm.ValidationError);
    }

    [Fact]
    public void Validate_WhenIntervalAtMax_ClearsError()
    {
        var vm = Build();
        vm.IntervalMinutes = 61;  // set bad first
        vm.IntervalMinutes = 60;
        Assert.Null(vm.ValidationError);
    }

    // ── schedule validation ───────────────────────────────────────────────────

    [Fact]
    public void Validate_ScheduleDisabled_InvalidTimes_NoError()
    {
        var vm = Build();
        vm.ScheduleEnabled = false;
        vm.ScheduleStart = "not-a-time";
        Assert.Null(vm.ValidationError);
    }

    [Fact]
    public void Validate_ScheduleEnabled_BadStartFormat_SetsError()
    {
        var vm = Build();
        vm.ScheduleEnabled = true;
        vm.ScheduleStart = "25:00";
        Assert.NotNull(vm.ValidationError);
        Assert.Contains("Start time", vm.ValidationError);
    }

    [Fact]
    public void Validate_ScheduleEnabled_BadEndFormat_SetsError()
    {
        var vm = Build();
        vm.ScheduleEnabled = true;
        vm.ScheduleStart = "09:00";
        vm.ScheduleEnd = "bad";
        Assert.NotNull(vm.ValidationError);
        Assert.Contains("End time", vm.ValidationError);
    }

    [Fact]
    public void Validate_ScheduleEnabled_StartEqualsEnd_SetsError()
    {
        var vm = Build();
        vm.ScheduleEnabled = true;
        vm.ScheduleStart = "09:00";
        vm.ScheduleEnd = "09:00";
        Assert.NotNull(vm.ValidationError);
        Assert.Contains("before end", vm.ValidationError);
    }

    [Fact]
    public void Validate_ScheduleEnabled_StartAfterEnd_SetsError()
    {
        var vm = Build();
        vm.ScheduleEnabled = true;
        vm.ScheduleStart = "18:00";
        vm.ScheduleEnd = "09:00";
        Assert.NotNull(vm.ValidationError);
        Assert.Contains("before end", vm.ValidationError);
    }

    [Fact]
    public void Validate_ScheduleEnabled_NoDaysSelected_SetsError()
    {
        var vm = Build();
        vm.ScheduleEnabled = true;
        vm.ScheduleStart = "09:00";
        vm.ScheduleEnd = "18:00";
        vm.Monday = false;
        vm.Tuesday = false;
        vm.Wednesday = false;
        vm.Thursday = false;
        vm.Friday = false;
        vm.Saturday = false;
        vm.Sunday = false;
        Assert.NotNull(vm.ValidationError);
        Assert.Contains("one active day", vm.ValidationError);
    }

    [Fact]
    public void Validate_ScheduleEnabled_AllValid_NoError()
    {
        var vm = Build();
        vm.ScheduleEnabled = true;
        vm.ScheduleStart = "09:00";
        vm.ScheduleEnd = "18:00";
        vm.Monday = true;
        Assert.Null(vm.ValidationError);
    }

    // ── CanSave ───────────────────────────────────────────────────────────────

    [Fact]
    public void CanSave_WhenValidationError_ReturnsFalse()
    {
        var vm = Build();
        vm.IntervalMinutes = 0;
        Assert.False(vm.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void CanSave_WhenValid_ReturnsTrue()
    {
        var vm = Build();
        Assert.True(vm.SaveCommand.CanExecute(null));
    }

    // ── LoadAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoadAsync_PopulatesInterval()
    {
        var vm = Build(new BlinkSettings { ReminderIntervalMinutes = 5 });
        await vm.LoadAsync();
        Assert.Equal(5, vm.IntervalMinutes);
    }

    [Fact]
    public async Task LoadAsync_PopulatesActiveDays()
    {
        var vm = Build(new BlinkSettings
        {
            ActiveDays = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday],
        });
        await vm.LoadAsync();
        Assert.True(vm.Monday);
        Assert.False(vm.Tuesday);
        Assert.True(vm.Wednesday);
        Assert.False(vm.Thursday);
        Assert.True(vm.Friday);
        Assert.False(vm.Saturday);
        Assert.False(vm.Sunday);
    }

    [Fact]
    public async Task LoadAsync_PopulatesScheduleFields()
    {
        var vm = Build(new BlinkSettings
        {
            ScheduleEnabled = true,
            ScheduleStartTime = TimeSpan.FromHours(8),
            ScheduleEndTime = TimeSpan.FromHours(17),
        });
        await vm.LoadAsync();
        Assert.True(vm.ScheduleEnabled);
        Assert.Equal("08:00", vm.ScheduleStart);
        Assert.Equal("17:00", vm.ScheduleEnd);
    }

    // ── test doubles ──────────────────────────────────────────────────────────

    private sealed class FakeSettingsStore(BlinkSettings initial) : ISettingsStore
    {
        public Task<BlinkSettings> LoadAsync(CancellationToken ct = default)
            => Task.FromResult(initial);

        public Task SaveAsync(BlinkSettings s, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class FakeStartupRegistrar : IStartupRegistrar
    {
        public Task EnableAsync() => Task.CompletedTask;
        public Task DisableAsync() => Task.CompletedTask;
    }

    private sealed class FakeTimer : ITimerControl
    {
        public void Start() { }
        public void Stop() { }
        public void ResetTimer() { }
    }
}
