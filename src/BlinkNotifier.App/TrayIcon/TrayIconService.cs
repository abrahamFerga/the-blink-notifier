// BlinkNotifier.App — Hardcodet NotifyIcon wrapper (ARCH.md § Solution layout, ADR-0002, #6)
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.Logging;

namespace BlinkNotifier.App.TrayIcon;

public sealed class TrayIconService : IDisposable
{
    private readonly TrayIconViewModel _viewModel;
    private readonly ILogger<TrayIconService> _logger;
    private TaskbarIcon? _taskbarIcon;

    private readonly Icon _iconActive;
    private readonly Icon _iconPaused;
    private readonly Icon _iconStopped;

    public TrayIconService(TrayIconViewModel viewModel, ILogger<TrayIconService> logger)
    {
        _viewModel = viewModel;
        _logger = logger;

        _iconActive = CreateCircleIcon(Color.FromArgb(0x26, 0x8B, 0xD2)); // blue
        _iconPaused = CreateCircleIcon(Color.FromArgb(0xCB, 0x4B, 0x16)); // orange
        _iconStopped = CreateCircleIcon(Color.FromArgb(0x58, 0x58, 0x58)); // grey
    }

    public void Initialize()
    {
        _taskbarIcon = new TaskbarIcon
        {
            Icon = _iconActive,
            ToolTipText = _viewModel.ToolTip,
        };

        // Accessibility: expose accessible name via UIA (WCAG 2.1 SC 4.1.2)
        AutomationProperties.SetName(_taskbarIcon, "Blink Notifier");

        _taskbarIcon.ContextMenu = BuildContextMenu();
        _taskbarIcon.DataContext = _viewModel;

        // Capture dispatcher once — FullscreenPoller fires TrayIconStateChanged on a
        // background thread, and DependencyObject.SetValue requires the owner thread.
        var dispatcher = _taskbarIcon.Dispatcher;

        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(TrayIconViewModel.ToolTip) &&
                e.PropertyName != nameof(TrayIconViewModel.IsRunning))
                return;
            dispatcher.Invoke(() =>
            {
                if (e.PropertyName == nameof(TrayIconViewModel.ToolTip))
                    _taskbarIcon.ToolTipText = _viewModel.ToolTip;
                if (e.PropertyName == nameof(TrayIconViewModel.IsRunning))
                    _taskbarIcon.Icon = _viewModel.IsPaused ? _iconPaused
                                      : _viewModel.IsRunning ? _iconActive
                                      : _iconStopped;
            });
        };

        _viewModel.TrayIconStateChanged += isPaused =>
            dispatcher.Invoke(() =>
                _taskbarIcon.Icon = isPaused ? _iconPaused
                                  : _viewModel.IsRunning ? _iconActive
                                  : _iconStopped);

        _logger.LogInformation("Tray icon initialised.");
    }

    private ContextMenu BuildContextMenu()
    {
        var menu = new ContextMenu();

        menu.Items.Add(new MenuItem { Header = "Start", Command = _viewModel.StartCommand });
        menu.Items.Add(new MenuItem { Header = "Stop", Command = _viewModel.StopCommand });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "Settings", Command = _viewModel.SettingsCommand });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "Exit", Command = _viewModel.ExitCommand });

        return menu;
    }

    private static Icon CreateCircleIcon(Color color)
    {
        using var bmp = new Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.FillEllipse(new SolidBrush(color), 1, 1, 13, 13);

        // GetHicon returns a Win32 HICON we must destroy; Clone copies it into managed memory.
        var hIcon = bmp.GetHicon();
        var icon = (Icon)Icon.FromHandle(hIcon).Clone();
        DestroyIcon(hIcon);
        return icon;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(nint hIcon);

    public void Dispose()
    {
        _taskbarIcon?.Dispose();
        _iconActive.Dispose();
        _iconPaused.Dispose();
        _iconStopped.Dispose();
    }
}
