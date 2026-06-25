// BlinkNotifier.App — settings window code-behind (ARCH.md § Solution layout, #11)
using System.Windows;

namespace BlinkNotifier.App.Settings;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        Loaded += async (_, _) => await _viewModel.LoadAsync();
        _viewModel.SaveSucceeded += (_, _) => Close();
    }
}
