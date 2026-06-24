// BlinkNotifier.App — WPF Application root (ARCH.md § Solution layout, ADR-0001, ADR-0007)
using System.IO;
using System.Threading;
using System.Windows;
using BlinkNotifier.App.Settings;
using BlinkNotifier.App.Startup;
using BlinkNotifier.App.TrayIcon;
using BlinkNotifier.App.Wizard;
using BlinkNotifier.Core;
using BlinkNotifier.Core.Timer;
using BlinkNotifier.Core.Toast;
using BlinkNotifier.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace BlinkNotifier.App;

public partial class App : Application
{
    private IHost? _host;
    private Mutex? _singleInstanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Single-instance enforcement (ADR-0006)
        _singleInstanceMutex = new Mutex(true, "Global\\BlinkNotifier-SingleInstance", out bool isNew);
        if (!isNew)
        {
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
            Shutdown(0);
            return;
        }

        // Bootstrap Serilog before host build so early errors are captured (ADR-0010)
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlinkNotifier", "logs", "blink-.json");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
            .WriteTo.EventLog("Blink Notifier", manageEventSource: false,
                restrictedToMinimumLevel: LogEventLevel.Error)
            .CreateLogger();

        // Catch-all for background-thread faults — log then let the runtime terminate.
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            Log.Fatal((Exception)args.ExceptionObject, "Unhandled background thread exception.");
            Log.CloseAndFlush();
        };

        // Catch-all for UI-thread faults — log, show a brief message, keep the app running.
        DispatcherUnhandledException += (_, args) =>
        {
            Log.Fatal(args.Exception, "Unhandled dispatcher exception.");
            args.Handled = true;
            MessageBox.Show(
                $"An unexpected error occurred:\n{args.Exception.Message}\n\nBlink Notifier will continue running.",
                "Blink Notifier — Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        };

        // Generic Host (ADR-0007)
        var builder = Host.CreateApplicationBuilder(e.Args);
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger, dispose: true);

        builder.Services.AddBlinkSettings();
        builder.Services.AddBlinkCore();
        builder.Services.AddSingleton<TrayIconViewModel>();
        builder.Services.AddSingleton<TrayIconService>();
        builder.Services.AddTransient<SettingsWindow>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<StartupRegistrar>();
        builder.Services.AddTransient<FirstRunWizard>();

        _host = builder.Build();

        // Wire toast activation before starting background services (ADR-0003, ADR-0005)
        var snooze = _host.Services.GetRequiredService<SnoozeStateMachine>();
        var timer = _host.Services.GetRequiredService<ReminderTimerService>();
        var logger = _host.Services.GetRequiredService<ILogger<App>>();
        ToastActivationHandler.Register(snooze, timer, logger);

        // Bridge host shutdown to WPF application shutdown
        _host.Services.GetRequiredService<IHostApplicationLifetime>()
            .ApplicationStopping.Register(() => Dispatcher.Invoke(Shutdown));

        _host.Start();

        var trayService = _host.Services.GetRequiredService<TrayIconService>();
        trayService.Initialize();
        _host.Services.GetRequiredService<TrayIconViewModel>().SettingsRequested += ShowSettings;

        if (!e.Args.Contains("--startup") && IsFirstRun())
            _host.Services.GetRequiredService<FirstRunWizard>().ShowDialog();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Services.GetRequiredService<TrayIconService>().Dispose();
        _host?.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        _host?.Dispose(); // disposes IServiceProvider → calls Dispose() on IDisposable singletons
        Log.CloseAndFlush();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private static bool IsFirstRun()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlinkNotifier",
            "settings.json");
        return !File.Exists(path);
    }

    private void ShowSettings()
        => _host!.Services.GetRequiredService<SettingsWindow>().ShowDialog();
}
