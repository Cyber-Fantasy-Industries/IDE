using System;
using System.Windows.Input;

namespace GatewayIDE.App.Views.Settings;

public sealed class SettingsPanelCommands
{
    private readonly SettingsPanelState _state;

    public SettingsPanelCommands(SettingsPanelState state)
    {
        _state = state;

        SaveCommand = new RelayCommand(() =>
        {
            // TODO: persist SettingsService etc.
            Console.WriteLine("[Settings] Save");
        });

        ResetCommand = new RelayCommand(() =>
        {
            // TODO: reset to defaults
            Console.WriteLine("[Settings] Reset");
        });

        ReloadServiceSettingsCommand = new RelayCommand(() =>
        {
            Console.WriteLine("[Settings] Reload service settings");
        });

        ApplyServiceSettingsCommand = new RelayCommand(() =>
        {
            Console.WriteLine("[Settings] Apply service settings");
        });

        OpenServiceLogsCommand = new RelayCommand(() =>
        {
            Console.WriteLine("[Settings] Open logs");
        });
    }

    public ICommand SaveCommand { get; }
    public ICommand ResetCommand { get; }

    public ICommand ReloadServiceSettingsCommand { get; }
    public ICommand ApplyServiceSettingsCommand { get; }
    public ICommand OpenServiceLogsCommand { get; }
}

/// <summary>
/// Tiny ICommand helper (no deps).
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
