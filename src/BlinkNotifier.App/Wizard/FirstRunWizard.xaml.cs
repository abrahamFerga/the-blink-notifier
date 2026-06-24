// BlinkNotifier.App — first-run wizard code-behind (ARCH.md § Solution layout, #7)
using System.Windows;
using BlinkNotifier.App.Startup;
using Microsoft.Extensions.Logging;

namespace BlinkNotifier.App.Wizard;

public partial class FirstRunWizard : Window
{
    private readonly StartupRegistrar _startup;
    private readonly ILogger<FirstRunWizard> _logger;

    public FirstRunWizard(StartupRegistrar startup, ILogger<FirstRunWizard> logger)
    {
        _startup = startup;
        _logger  = logger;
        InitializeComponent();
    }

    private async void OnFinish(object sender, RoutedEventArgs e)
    {
        if (AutoLaunchCheckBox.IsChecked == true)
        {
            try { await _startup.EnableAsync(); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to register startup; user can enable it later in Settings.");
            }
        }

        Close();
    }
}
