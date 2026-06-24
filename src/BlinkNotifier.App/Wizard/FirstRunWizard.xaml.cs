// BlinkNotifier.App — first-run wizard code-behind (ARCH.md § Solution layout, #7)
using System.Windows;
using BlinkNotifier.App.Startup;

namespace BlinkNotifier.App.Wizard;

public partial class FirstRunWizard : Window
{
    private readonly StartupRegistrar _startup;

    public FirstRunWizard(StartupRegistrar startup)
    {
        _startup = startup;
        InitializeComponent();
    }

    private async void OnFinish(object sender, RoutedEventArgs e)
    {
        if (AutoLaunchCheckBox.IsChecked == true)
            await _startup.EnableAsync();

        Close();
    }
}
