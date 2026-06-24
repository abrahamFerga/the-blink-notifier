// BlinkNotifier.App — shared ICommand implementation
using System.Windows.Input;

namespace BlinkNotifier.App.Commands;

internal sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public bool CanExecute(object? p) => canExecute?.Invoke() ?? true;
    public void Execute(object? p) => execute();
    public event EventHandler? CanExecuteChanged
    {
        add    => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
