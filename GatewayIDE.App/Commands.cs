using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using GatewayIDE.App.Views;

namespace GatewayIDE.App.Commands;

public sealed class MainCommands
{
    private readonly LayoutState _layout;

#if !ISOLATION_MODE
    private readonly GatewayIDE.App.Views.Chat.ChatState _chat;
    private readonly GatewayIDE.App.Views.KiSystem.ThreadRouter _threads;
#endif

    public ICommand ToggleChatCommand { get; }
    public ICommand SelectTabCommand { get; }
    public ICommand MenuActionCommand { get; }
    public ICommand SendChatCommand { get; }

#if ISOLATION_MODE
    public MainCommands(LayoutState layout)
    {
        _layout = layout;

        // Im Isolation Mode: Commands existieren, tun aber nichts Feature-spezifisches.
        ToggleChatCommand = new AsyncCommand(_ => Task.CompletedTask);
        SendChatCommand   = new AsyncCommand(_ => Task.CompletedTask);

        MenuActionCommand = new AsyncCommand(p =>
        {
            var id = p?.ToString();
            if (!string.IsNullOrWhiteSpace(id))
                Console.WriteLine($"[MENU/ISOLATION] {id}");
            return Task.CompletedTask;
        });

        SelectTabCommand = new AsyncCommand(p =>
        {
            _layout.Select(p?.ToString());
            return Task.CompletedTask;
        });
    }
#else
    public MainCommands(
        LayoutState layout,
        GatewayIDE.App.Views.Chat.ChatState chat,
        GatewayIDE.App.Views.KiSystem.ThreadRouter threads)
    {
        _layout = layout;
        _chat = chat;
        _threads = threads;

        ToggleChatCommand = new AsyncCommand(_ => _chat.ToggleChatSidebar());
        SendChatCommand   = new AsyncCommand(async _ => await _chat.SendAsync());

        MenuActionCommand = new AsyncCommand(p => OnMenuAction(p?.ToString()));

        SelectTabCommand = new AsyncCommand(p =>
        {
            _layout.Select(p?.ToString());
            return Task.CompletedTask;
        });
    }

    private void OnMenuAction(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        var msg = $"[MENU] {id}";
        Console.WriteLine(msg);

        try { _threads.Append(ThreadId.T1, msg); } catch { }
    }
#endif
}

public sealed class AsyncCommand : ICommand
{
    private readonly Action<object?>? _execute;
    private readonly Func<object?, Task>? _executeAsync;
    private readonly Func<object?, bool>? _canExecute;
    private int _isRunning;

    public event EventHandler? CanExecuteChanged;

    public AsyncCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

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
            try { await _executeAsync(parameter); }
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
