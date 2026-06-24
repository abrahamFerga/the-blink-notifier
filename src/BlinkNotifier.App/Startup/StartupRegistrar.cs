// BlinkNotifier.App — Windows startup registration (ARCH.md § Solution layout, ADR-0005, #7)
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace BlinkNotifier.App.Startup;

public sealed class StartupRegistrar(ILogger<StartupRegistrar> logger)
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunKeyName = "BlinkNotifier";

    /// <summary>
    /// Returns true when the current process runs inside an MSIX package.
    /// Avoids pulling in WinAppSdk just for this check.
    /// </summary>
    private static bool IsPackaged
    {
        get
        {
            try
            {
                // GetCurrentPackageFullName returns 0 (success) when packaged.
                return Windows.ApplicationModel.Package.Current != null;
            }
            catch
            {
                return false;
            }
        }
    }

    public async Task EnableAsync()
    {
        if (IsPackaged)
        {
            await EnableMsixStartupTaskAsync();
        }
        else
        {
            EnableRegistryRunKey();
        }
    }

    public async Task DisableAsync()
    {
        if (IsPackaged)
        {
            await DisableMsixStartupTaskAsync();
        }
        else
        {
            DisableRegistryRunKey();
        }
    }

    private static async Task EnableMsixStartupTaskAsync()
    {
        var task = await Windows.ApplicationModel.StartupTask.GetAsync("BlinkNotifierStartup");
        if (task.State is Windows.ApplicationModel.StartupTaskState.Disabled
                       or Windows.ApplicationModel.StartupTaskState.DisabledByUser)
        {
            await task.RequestEnableAsync();
        }
    }

    private static async Task DisableMsixStartupTaskAsync()
    {
        var task = await Windows.ApplicationModel.StartupTask.GetAsync("BlinkNotifierStartup");
        if (task.State is Windows.ApplicationModel.StartupTaskState.Enabled
                       or Windows.ApplicationModel.StartupTaskState.EnabledByPolicy)
        {
            task.Disable();
        }
    }

    private void EnableRegistryRunKey()
    {
        var exePath = Environment.ProcessPath
            ?? System.Reflection.Assembly.GetExecutingAssembly().Location;

        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);

        key.SetValue(RunKeyName, $"\"{exePath}\" --startup");
        logger.LogInformation("Startup Run key registered for portable EXE.");
    }

    private void DisableRegistryRunKey()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(RunKeyName, throwOnMissingValue: false);
        logger.LogInformation("Startup Run key removed.");
    }
}
