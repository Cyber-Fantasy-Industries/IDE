using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GatewayIDE.App.ViewModels;

/// <summary>
/// Minimal ICommand helper.
/// Unterstützt sync + async Actions.
/// </summary>
public sealed class DelegateCommand : ICommand
{
    private readonly Action<object?>? _execute;
    private readonly Func<object?, Task>? _executeAsync;
    private readonly Func<object?, bool>? _canExecute;
    private int _isRunning; // 0/1

    public event EventHandler? CanExecuteChanged;

    // Sync
    public DelegateCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    // Async
    public DelegateCommand(Func<object?, Task> executeAsync, Func<object?, bool>? canExecute = null)
    {
        _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;

        if (_execute != null)
        {
            _execute(parameter);
            return;
        }

        if (_executeAsync != null)
        {
            // Prevent reentrancy / double clicks
            if (System.Threading.Interlocked.Exchange(ref _isRunning, 1) == 1)
                return;

            try { await _executeAsync(parameter); }
            catch
            {
                // bewusst: keine UI-Dependencies hier.
                // Fehler laufen als Exceptions im Debugger auf, oder werden im jeweiligen Service geloggt.
                throw;
            }
            finally
            {
                System.Threading.Interlocked.Exchange(ref _isRunning, 0);
            }
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
