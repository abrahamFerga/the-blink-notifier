// BlinkNotifier.App — tray icon view model (ARCH.md § Solution layout, #6)
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BlinkNotifier.App.Commands;
using BlinkNotifier.Core.Fullscreen;
using BlinkNotifier.Core.Timer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BlinkNotifier.App.TrayIcon;

public sealed class TrayIconViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ReminderTimerService _timer;
    private readonly FullscreenState _fullscreen;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<TrayIconViewModel> _logger;

    private bool _isRunning = true;
    private bool _isPaused;

    public TrayIconViewModel(
        ReminderTimerService timer,
        FullscreenState fullscreen,
        IHostApplicationLifetime lifetime,
        ILogger<TrayIconViewModel> logger)
    {
        _timer = timer;
        _fullscreen = fullscreen;
        _lifetime = lifetime;
        _logger = logger;

        _fullscreen.FullscreenChanged += OnFullscreenChanged;

        StartCommand = new RelayCommand(OnStart, () => !_isRunning);
        StopCommand = new RelayCommand(OnStop, () => _isRunning);
        SettingsCommand = new RelayCommand(OnSettings);
        ExitCommand = new RelayCommand(OnExit);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set { _isRunning = value; OnPropertyChanged(); OnPropertyChanged(nameof(ToolTip)); }
    }

    public bool IsPaused
    {
        get => _isPaused;
        private set { _isPaused = value; OnPropertyChanged(); OnPropertyChanged(nameof(ToolTip)); }
    }

    public string ToolTip => IsPaused
        ? "Blink Notifier — PAUSED (fullscreen)"
        : IsRunning
            ? "Blink Notifier — Active"
            : "Blink Notifier — Stopped";

    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand SettingsCommand { get; }
    public ICommand ExitCommand { get; }

    public event Action? SettingsRequested;
    public event Action<bool>? TrayIconStateChanged; // true = paused

    private void OnStart()
    {
        _timer.Start();
        IsRunning = true;
        _logger.LogInformation("Timer started by user.");
    }

    private void OnStop()
    {
        _timer.Stop();
        IsRunning = false;
        _logger.LogInformation("Timer stopped by user.");
    }

    private void OnSettings() => SettingsRequested?.Invoke();

    private void OnExit() => _lifetime.StopApplication();

    private void OnFullscreenChanged(object? sender, bool active)
    {
        IsPaused = active;
        TrayIconStateChanged?.Invoke(active);
    }

    public void Dispose() => _fullscreen.FullscreenChanged -= OnFullscreenChanged;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
