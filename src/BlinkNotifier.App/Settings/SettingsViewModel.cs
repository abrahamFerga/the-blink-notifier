// BlinkNotifier.App — settings window ViewModel (ARCH.md § Solution layout, Epic 3, #11)
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BlinkNotifier.App.Commands;
using BlinkNotifier.App.Startup;
using BlinkNotifier.Settings;
using Microsoft.Extensions.Logging;

namespace BlinkNotifier.App.Settings;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly ISettingsStore _store;
    private readonly StartupRegistrar _startup;
    private readonly ILogger<SettingsViewModel> _logger;

    private int _intervalMinutes = 20;
    private bool _autoLaunchEnabled = true;
    private string? _validationError;

    public SettingsViewModel(
        ISettingsStore store,
        StartupRegistrar startup,
        ILogger<SettingsViewModel> logger)
    {
        _store   = store;
        _startup = startup;
        _logger  = logger;

        SaveCommand = new RelayCommand(async () => await SaveAsync(), CanSave);
    }

    public int IntervalMinutes
    {
        get => _intervalMinutes;
        set
        {
            _intervalMinutes = value;
            OnPropertyChanged();
            ValidateInterval();
        }
    }

    public bool AutoLaunchEnabled
    {
        get => _autoLaunchEnabled;
        set { _autoLaunchEnabled = value; OnPropertyChanged(); }
    }

    public string? ValidationError
    {
        get => _validationError;
        private set { _validationError = value; OnPropertyChanged(); }
    }

    public ICommand SaveCommand { get; }

    public async Task LoadAsync()
    {
        var s = await _store.LoadAsync();
        IntervalMinutes    = s.ReminderIntervalMinutes;
        AutoLaunchEnabled  = s.AutoLaunchEnabled;
    }

    private bool CanSave() => string.IsNullOrEmpty(ValidationError);

    private async Task SaveAsync()
    {
        var current = await _store.LoadAsync();
        var updated = new BlinkSettings
        {
            SchemaVersion          = current.SchemaVersion,
            ReminderIntervalMinutes = IntervalMinutes,
            ScheduleEnabled        = current.ScheduleEnabled,
            ScheduleStartTime      = current.ScheduleStartTime,
            ScheduleEndTime        = current.ScheduleEndTime,
            ActiveDays             = current.ActiveDays,
            AutoLaunchEnabled      = AutoLaunchEnabled,
            SnoozeOptionsMinutes   = current.SnoozeOptionsMinutes,
        };

        await _store.SaveAsync(updated);

        if (AutoLaunchEnabled)
            await _startup.EnableAsync();
        else
            await _startup.DisableAsync();

        _logger.LogInformation("Settings saved — interval={Interval}m, autoLaunch={AutoLaunch}.",
            IntervalMinutes, AutoLaunchEnabled);
    }

    private void ValidateInterval() =>
        ValidationError = _intervalMinutes is < 1 or > 60
            ? "Interval must be between 1 and 60 minutes."
            : null;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
