using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GatewayIDE.App.Commands;

/// <summary>
/// App-weite Commands.
///
/// Wichtig: Diese Datei ist absichtlich "refactor-resistent":
/// - Keine harten Abhängigkeiten auf volatile UI-Typen (z.B. ServiceUnitVm)
/// - Keine harten Abhängigkeiten auf LayoutState-Konstanten
/// Dadurch bekommst du deutlich weniger CS0246/Namespace-Drift beim Umbenennen/Verschieben.
/// </summary>
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

    private readonly GatewayIDE.App.MainState _state;

    public MainCommands(GatewayIDE.App.MainState state)
    {
        _state = state;

        ToggleChatCommand =
            new AsyncCommand(_ => _state.Chat.ToggleChatSidebar());

        SelectTabCommand =
            new AsyncCommand(async p =>
            {
                // kein LayoutState.TAB_* mehr (zu volatil) -> fallback auf bestehenden ActiveTab oder "dash"
                var tab = p?.ToString();
                if (string.IsNullOrWhiteSpace(tab))
                    tab = _state.Layout.ActiveTab ?? "dash";

                _state.Layout.ActiveTab = tab;

                if (_state.Layout.IsDocker)
                    await _state.Docker.Controller.RefreshSystemStatusAsync();
            });

        SendChatCommand =
            new AsyncCommand(async _ => await _state.Chat.SendAsync());

        MenuActionCommand =
            new AsyncCommand(p => OnMenuAction(p?.ToString()));

        RebuildGatewayCommand =
            new AsyncCommand(async p =>
            {
                SelectUnitIfProvided(_state, p);
                await _state.Docker.Controller.UnitFullRebuildAsync();
            });

        StartGatewayCommand =
            new AsyncCommand(async p =>
            {
                SelectUnitIfProvided(_state, p);
                await _state.Docker.Controller.UnitStartAsync();
            });

        StopGatewayCommand =
            new AsyncCommand(async p =>
            {
                SelectUnitIfProvided(_state, p);
                await _state.Docker.Controller.UnitStopAsync();
            });

        RestartGatewayCommand =
            new AsyncCommand(async p =>
            {
                SelectUnitIfProvided(_state, p);
                await _state.Docker.Controller.UnitRestartAsync();
            });

        RemoveGatewayContainerCommand =
            new AsyncCommand(async p =>
            {
                SelectUnitIfProvided(_state, p);
                await _state.Docker.Controller.UnitRemoveContainerAsync();
            });

        ClearAllLogsCommand =
            new AsyncCommand(_ =>
            {
                foreach (var u in _state.Docker.Units.Units)
                    u.Logs.ClearAll();
            });

        ClearSelectedLogsCommand =
            new AsyncCommand(p =>
            {
                SelectUnitIfProvided(_state, p);
                _state.Docker.Units.SelectedUnit?.Logs.ClearAll();
            });

        OpenUnitCommand =
            new AsyncCommand(async p =>
            {
                SelectUnitIfProvided(_state, p);
                await _state.Docker.Controller.RefreshSystemStatusAsync();
                _state.Docker.Controller.StartTailLogs();
            });

        ExecuteInContainerCommand =
            new AsyncCommand(async p => await _state.Docker.Controller.ExecuteInContainerAsync(p as string));
    }

    private static void SelectUnitIfProvided(global::GatewayIDE.App.MainState state, object? parameter)
    {
        if (parameter is null) return;

        // state.Docker.Units.SelectedUnit ist typisiert (z.B. ServiceUnitVm).
        // Damit wir nicht hart gegen diesen Typ linken, setzen wir per Reflection,
        // aber nur wenn der Parametertyp passt.
        var units = state.Docker?.Units;
        if (units is null) return;

        var prop = units.GetType().GetProperty("SelectedUnit", BindingFlags.Instance | BindingFlags.Public);
        if (prop is null || !prop.CanWrite) return;

        if (prop.PropertyType.IsInstanceOfType(parameter))
            prop.SetValue(units, parameter);
    }

    private void OnMenuAction(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return;

        var msg = $"[MENU] {id}";

        // Optional: wenn Threads.Append vorhanden ist, versuchen wir zu schreiben,
        // ohne harte ThreadId-Abhängigkeit.
        TryAppendToThreads(_state, msg);

        Console.WriteLine(msg);
    }

    private static void TryAppendToThreads(global::GatewayIDE.App.MainState state, string msg)
    {
        var threadsObj = (object?)state.GetType().GetProperty("Threads", BindingFlags.Instance | BindingFlags.Public)?.GetValue(state);
        if (threadsObj is null) return;

        // 1) Append(string, string)
        var appendStr = threadsObj.GetType().GetMethod("Append", new[] { typeof(string), typeof(string) });
        if (appendStr != null)
        {
            appendStr.Invoke(threadsObj, new object[] { "T1", msg });
            return;
        }

        // 2) Append(ThreadIdEnum, string) oder ähnliches
        var candidates = threadsObj.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.Name == "Append" && m.GetParameters().Length == 2)
            .ToList();

        foreach (var m in candidates)
        {
            var ps = m.GetParameters();
            if (ps[1].ParameterType != typeof(string)) continue;

            var t = ps[0].ParameterType;
            if (!t.IsEnum) continue;

            // versuche Enum-Wert "T1"
            var names = Enum.GetNames(t);
            var t1Name = names.FirstOrDefault(n => string.Equals(n, "T1", StringComparison.OrdinalIgnoreCase));
            if (t1Name is null) continue;

            var t1Value = Enum.Parse(t, t1Name);
            m.Invoke(threadsObj, new object[] { t1Value, msg });
            return;
        }
    }
}

/// <summary>
/// Universeller ICommand:
/// - sync oder async
/// - verhindert Reentrancy
/// - deaktiviert CanExecute während async läuft
/// </summary>
public sealed class AsyncCommand : ICommand
{
    private readonly Action<object?>? _execute;
    private readonly Func<object?, Task>? _executeAsync;
    private readonly Func<object?, bool>? _canExecute;

    private int _isRunning;

    public event EventHandler? CanExecuteChanged;

    // Sync
    public AsyncCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    // Async
    public AsyncCommand(Func<object?, Task> executeAsync, Func<object?, bool>? canExecute = null)
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
