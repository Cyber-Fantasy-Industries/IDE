using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using GatewayIDE.App.Views;

#if FEATURE_CHAT
using GatewayIDE.App.Views.Chat;
#endif

#if FEATURE_KISYSTEM
using GatewayIDE.App.Views.KiSystem;
#endif

namespace GatewayIDE.App.Commands;

public sealed class MainCommands
{
    private readonly LayoutState _layout;

#if !ISOLATION_MODE && FEATURE_CHAT
    private readonly ChatState _chat;
#endif

#if !ISOLATION_MODE && FEATURE_KISYSTEM
    private readonly ThreadRouter _threads;
#endif

    public ICommand ToggleChatCommand { get; }
    public ICommand SelectTabCommand { get; }
    public ICommand MenuActionCommand { get; }
    public ICommand SendChatCommand { get; }

    public MainCommands(
        LayoutState layout
#if !ISOLATION_MODE && FEATURE_CHAT
        , ChatState chat
#endif
#if !ISOLATION_MODE && FEATURE_KISYSTEM
        , ThreadRouter threads
#endif
        )
    {
        _layout = layout;

#if !ISOLATION_MODE && FEATURE_CHAT
        _chat = chat;
        ToggleChatCommand = new AsyncCommand(_ => _chat.ToggleChatSidebar());
        SendChatCommand   = new AsyncCommand(async _ => await _chat.SendAsync());
#else
        ToggleChatCommand = new AsyncCommand(_ => Task.CompletedTask);
        SendChatCommand   = new AsyncCommand(_ => Task.CompletedTask);
#endif

#if !ISOLATION_MODE && FEATURE_KISYSTEM
        _threads = threads;
#endif

        MenuActionCommand = new AsyncCommand(p => OnMenuAction(p?.ToString()));

        SelectTabCommand = new AsyncCommand(p =>
        {
            _layout.Select(p?.ToString());
            return Task.CompletedTask;
        });
    }

    private Task OnMenuAction(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Task.CompletedTask;

        var msg = $"[MENU] {id}";
        Console.WriteLine(msg);

#if !ISOLATION_MODE && FEATURE_KISYSTEM
        // Optional: wenn du einen konkreten API-Call hast, hier rein.
        // Ich lasse es bewusst "soft", damit es nie wieder bricht.
        try { /* _threads.Append(...); */ } catch { }
#endif

        return Task.CompletedTask;
    }
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
