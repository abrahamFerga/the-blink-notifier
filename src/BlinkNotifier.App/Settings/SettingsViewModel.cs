// BlinkNotifier.App — settings window ViewModel (ARCH.md § Solution layout, Epic 3, #11, #12)
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

    private int    _intervalMinutes   = 20;
    private bool   _autoLaunchEnabled = true;
    private bool   _scheduleEnabled   = false;
    private string _scheduleStart     = "09:00";
    private string _scheduleEnd       = "18:00";
    private bool   _monday    = true;
    private bool   _tuesday   = true;
    private bool   _wednesday = true;
    private bool   _thursday  = true;
    private bool   _friday    = true;
    private bool   _saturday  = false;
    private bool   _sunday    = false;
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

    // --- Interval ---
    public int IntervalMinutes
    {
        get => _intervalMinutes;
        set { _intervalMinutes = value; OnPropertyChanged(); Validate(); }
    }

    // --- Auto-launch ---
    public bool AutoLaunchEnabled
    {
        get => _autoLaunchEnabled;
        set { _autoLaunchEnabled = value; OnPropertyChanged(); }
    }

    // --- Schedule ---
    public bool ScheduleEnabled
    {
        get => _scheduleEnabled;
        set { _scheduleEnabled = value; OnPropertyChanged(); Validate(); }
    }

    public string ScheduleStart
    {
        get => _scheduleStart;
        set { _scheduleStart = value; OnPropertyChanged(); Validate(); }
    }

    public string ScheduleEnd
    {
        get => _scheduleEnd;
        set { _scheduleEnd = value; OnPropertyChanged(); Validate(); }
    }

    // --- Active days ---
    public bool Monday    { get => _monday;    set { _monday    = value; OnPropertyChanged(); } }
    public bool Tuesday   { get => _tuesday;   set { _tuesday   = value; OnPropertyChanged(); } }
    public bool Wednesday { get => _wednesday; set { _wednesday = value; OnPropertyChanged(); } }
    public bool Thursday  { get => _thursday;  set { _thursday  = value; OnPropertyChanged(); } }
    public bool Friday    { get => _friday;    set { _friday    = value; OnPropertyChanged(); } }
    public bool Saturday  { get => _saturday;  set { _saturday  = value; OnPropertyChanged(); } }
    public bool Sunday    { get => _sunday;    set { _sunday    = value; OnPropertyChanged(); } }

    // --- Validation ---
    public string? ValidationError
    {
        get => _validationError;
        private set { _validationError = value; OnPropertyChanged(); }
    }

    public ICommand SaveCommand { get; }

    public async Task LoadAsync()
    {
        var s = await _store.LoadAsync();
        IntervalMinutes   = s.ReminderIntervalMinutes;
        AutoLaunchEnabled = s.AutoLaunchEnabled;
        ScheduleEnabled   = s.ScheduleEnabled;
        _scheduleStart    = s.ScheduleStartTime.ToString(@"hh\:mm"); OnPropertyChanged(nameof(ScheduleStart));
        _scheduleEnd      = s.ScheduleEndTime.ToString(@"hh\:mm");   OnPropertyChanged(nameof(ScheduleEnd));
        var days = s.ActiveDays;
        _monday    = days.Contains(DayOfWeek.Monday);    OnPropertyChanged(nameof(Monday));
        _tuesday   = days.Contains(DayOfWeek.Tuesday);   OnPropertyChanged(nameof(Tuesday));
        _wednesday = days.Contains(DayOfWeek.Wednesday); OnPropertyChanged(nameof(Wednesday));
        _thursday  = days.Contains(DayOfWeek.Thursday);  OnPropertyChanged(nameof(Thursday));
        _friday    = days.Contains(DayOfWeek.Friday);    OnPropertyChanged(nameof(Friday));
        _saturday  = days.Contains(DayOfWeek.Saturday);  OnPropertyChanged(nameof(Saturday));
        _sunday    = days.Contains(DayOfWeek.Sunday);    OnPropertyChanged(nameof(Sunday));
    }

    private bool CanSave() => string.IsNullOrEmpty(ValidationError);

    private async Task SaveAsync()
    {
        try
        {
            var current = await _store.LoadAsync();
            var updated = new BlinkSettings
            {
                SchemaVersion           = current.SchemaVersion,
                ReminderIntervalMinutes = IntervalMinutes,
                AutoLaunchEnabled       = AutoLaunchEnabled,
                ScheduleEnabled         = ScheduleEnabled,
                ScheduleStartTime       = ParseTime(ScheduleStart),
                ScheduleEndTime         = ParseTime(ScheduleEnd),
                ActiveDays              = BuildActiveDays(),
                SnoozeOptionsMinutes    = current.SnoozeOptionsMinutes,
            };

            await _store.SaveAsync(updated);

            if (AutoLaunchEnabled)
                await _startup.EnableAsync();
            else
                await _startup.DisableAsync();

            _logger.LogInformation(
                "Settings saved — interval={Interval}m, schedule={Schedule}, autoLaunch={AutoLaunch}.",
                IntervalMinutes, ScheduleEnabled, AutoLaunchEnabled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings.");
            ValidationError = "Failed to save settings. Please try again.";
        }
    }

    private void Validate()
    {
        if (_intervalMinutes is < 1 or > 60)
        {
            ValidationError = "Interval must be between 1 and 60 minutes.";
            return;
        }

        if (ScheduleEnabled)
        {
            if (!TimeSpan.TryParseExact(ScheduleStart, @"hh\:mm", null, out var start) &&
                !TimeSpan.TryParseExact(ScheduleStart, @"h\:mm",  null, out start))
            {
                ValidationError = "Start time must be in HH:MM format (e.g. 09:00).";
                return;
            }
            if (!TimeSpan.TryParseExact(ScheduleEnd, @"hh\:mm", null, out var end) &&
                !TimeSpan.TryParseExact(ScheduleEnd, @"h\:mm",  null, out end))
            {
                ValidationError = "End time must be in HH:MM format (e.g. 18:00).";
                return;
            }
            if (start >= end)
            {
                ValidationError = "Start time must be before end time.";
                return;
            }
            if (!BuildActiveDays().Any())
            {
                ValidationError = "At least one active day must be selected.";
                return;
            }
        }

        ValidationError = null;
    }

    private static TimeSpan ParseTime(string value)
    {
        if (TimeSpan.TryParseExact(value, @"hh\:mm", null, out var ts)) return ts;
        if (TimeSpan.TryParseExact(value, @"h\:mm",  null, out ts))     return ts;
        return TimeSpan.Zero;
    }

    private DayOfWeek[] BuildActiveDays()
    {
        var days = new List<DayOfWeek>();
        if (Monday)    days.Add(DayOfWeek.Monday);
        if (Tuesday)   days.Add(DayOfWeek.Tuesday);
        if (Wednesday) days.Add(DayOfWeek.Wednesday);
        if (Thursday)  days.Add(DayOfWeek.Thursday);
        if (Friday)    days.Add(DayOfWeek.Friday);
        if (Saturday)  days.Add(DayOfWeek.Saturday);
        if (Sunday)    days.Add(DayOfWeek.Sunday);
        return [.. days];
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
