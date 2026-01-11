using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using GatewayIDE.App;                  // MainLayoutState
using GatewayIDE.App.ViewModels;        // MainWindowViewModel
using GatewayIDE.App.Views.Docker;      // ServiceUnitVm
using GatewayIDE.App.Views.KiSystem;    // ThreadId  <-- falls ThreadId dort liegt

namespace GatewayIDE.App.Commands;

/// <summary>
/// Zentrale Command-Sammlung für GatewayIDE.
/// </summary>
public static class Commands
{
    public sealed class MainCommands
    {
        public ICommand ToggleChatCommand { get; }
        public ICommand SelectTabCommand { get; }
        public ICommand SendChatCommand { get; }

        public ICommand MenuActionCommand { get; }

        public ICommand RebuildGatewayCommand { get; }
        public ICommand StartGatewayCommand { get; }
        public ICommand StopGatewayCommand { get; }
        public ICommand RestartGatewayCommand { get; }
        public ICommand RemoveGatewayContainerCommand { get; }
        public ICommand ClearAllLogsCommand { get; }
        public ICommand ClearSelectedLogsCommand { get; }
        public ICommand ExecuteInContainerCommand { get; }
        public ICommand OpenUnitCommand { get; }

        private readonly MainWindowViewModel _vm;

        private static void SelectUnitIfProvided(MainWindowViewModel vm, object? parameter)
        {
            if (parameter is ServiceUnitVm unit)
                vm.Docker.Units.SelectedUnit = unit;
        }

        public MainCommands(MainWindowViewModel vm)
        {
            _vm = vm;

            ToggleChatCommand =
                new Commands.Delegate(_ => vm.Chat.ToggleChatSidebar());

            SelectTabCommand =
                new Commands.Delegate(async p =>
                {
                    var tab = p?.ToString() ?? MainLayoutState.TAB_DASH;
                    vm.Layout.ActiveTab = tab;

                    if (vm.Layout.IsDocker)
                        await vm.Docker.Controller.RefreshSystemStatusAsync();
                });

            SendChatCommand =
                new Commands.Delegate(async _ => await vm.Chat.SendAsync());

            MenuActionCommand =
                new Commands.Delegate(p => OnMenuAction(p?.ToString()));

            RebuildGatewayCommand =
                new Commands.Delegate(async p =>
                {
                    SelectUnitIfProvided(vm, p);
                    await vm.Docker.Controller.UnitFullRebuildAsync();
                });

            StartGatewayCommand =
                new Commands.Delegate(async p =>
                {
                    SelectUnitIfProvided(vm, p);
                    await vm.Docker.Controller.UnitStartAsync();
                });

            StopGatewayCommand =
                new Commands.Delegate(async p =>
                {
                    SelectUnitIfProvided(vm, p);
                    await vm.Docker.Controller.UnitStopAsync();
                });

            RestartGatewayCommand =
                new Commands.Delegate(async p =>
                {
                    SelectUnitIfProvided(vm, p);
                    await vm.Docker.Controller.UnitRestartAsync();
                });

            RemoveGatewayContainerCommand =
                new Commands.Delegate(async p =>
                {
                    SelectUnitIfProvided(vm, p);
                    await vm.Docker.Controller.UnitRemoveContainerAsync();
                });

            ClearAllLogsCommand =
                new Commands.Delegate(_ =>
                {
                    foreach (var u in vm.Docker.Units.Units)
                        u.Logs.ClearAll();
                });

            ClearSelectedLogsCommand =
                new Commands.Delegate(p =>
                {
                    SelectUnitIfProvided(vm, p);
                    vm.Docker.Units.SelectedUnit?.Logs.ClearAll();
                });

            OpenUnitCommand =
                new Commands.Delegate(async p =>
                {
                    SelectUnitIfProvided(vm, p);
                    await vm.Docker.Controller.RefreshSystemStatusAsync();
                    vm.Docker.Controller.StartTailLogs();
                });

            ExecuteInContainerCommand =
                new Commands.Delegate(async p =>
                    await vm.Docker.Controller.ExecuteInContainerAsync(p as string));
        }

        private void OnMenuAction(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            _vm.Threads.Append(ThreadId.T1, $"[MENU] {id}");
            Console.WriteLine("[MENU] " + id);
        }
    }

    /// <summary>
    /// Universeller ICommand:
    /// - sync oder async
    /// - verhindert Reentrancy
    /// - deaktiviert CanExecute während async läuft
    /// </summary>
    public sealed class Delegate : ICommand
    {
        private readonly Action<object?>? _execute;
        private readonly Func<object?, Task>? _executeAsync;
        private readonly Func<object?, bool>? _canExecute;

        private int _isRunning;

        public event EventHandler? CanExecuteChanged;

        public Delegate(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public Delegate(Func<object?, Task> executeAsync, Func<object?, bool>? canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (_executeAsync != null && Volatile.Read(ref _isRunning) == 1)
                return false;

            return _canExecute?.Invoke(parameter) ?? true;
        }

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
                if (Interlocked.Exchange(ref _isRunning, 1) == 1)
                    return;

                RaiseCanExecuteChanged();

                try
                {
                    await _executeAsync(parameter);
                }
                finally
                {
                    Interlocked.Exchange(ref _isRunning, 0);
                    RaiseCanExecuteChanged();
                }
            }
        }

        public void RaiseCanExecuteChanged()
            => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
