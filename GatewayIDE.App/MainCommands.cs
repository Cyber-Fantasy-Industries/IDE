using System;
using System.Linq;
using System.Windows.Input;

namespace GatewayIDE.App.ViewModels;

public sealed class MainCommands
{
    public ICommand ExpandGatewayOnlyCommand { get; }

    public ICommand ToggleChatCommand { get; }
    public ICommand SelectTabCommand { get; }
    public ICommand SendChatCommand { get; }

    // ✅ Menu commands (Dropdown)
    public ICommand MenuActionCommand { get; }

    // Docker (unit-aware?if so renameing below)
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

        // Layout
        ExpandGatewayOnlyCommand =
            new DelegateCommand(_ => vm.Layout.ToggleExpandGatewayOnly());

        // Sidebar / Chat
        ToggleChatCommand =
            new DelegateCommand(_ => vm.Chat.ToggleChatSidebar());

        // Tabs (Dashboard removed)
        SelectTabCommand =
            new DelegateCommand(async p =>
            {
                var tab = p?.ToString() ?? MainLayoutState.TAB_DOCK;
                vm.Layout.ActiveTab = tab;

                // Refresh nur wenn Docker aktiv
                if (vm.Layout.IsDocker)
                    await vm.Docker.Controller.RefreshSystemStatusAsync();
            });

        SendChatCommand =
            new DelegateCommand(async _ => await vm.Chat.SendAsync());

        // ✅ Dropdown menu actions (File/Edit/Selection/Network/Settings)
        MenuActionCommand =
            new DelegateCommand(p => OnMenuAction(p?.ToString()));

        // Docker actions
        RebuildGatewayCommand =
            new DelegateCommand(async p =>
            {
                SelectUnitIfProvided(vm, p);
                await vm.Docker.Controller.UnitFullRebuildAsync();
            });

        StartGatewayCommand =
            new DelegateCommand(async p =>
            {
                SelectUnitIfProvided(vm, p);
                await vm.Docker.Controller.UnitStartAsync();
            });

        StopGatewayCommand =
            new DelegateCommand(async p =>
            {
                SelectUnitIfProvided(vm, p);
                await vm.Docker.Controller.UnitStopAsync();
            });

        RestartGatewayCommand =
            new DelegateCommand(async p =>
            {
                SelectUnitIfProvided(vm, p);
                await vm.Docker.Controller.UnitRestartAsync();
            });

        RemoveGatewayContainerCommand =
            new DelegateCommand(async p =>
            {
                SelectUnitIfProvided(vm, p);
                await vm.Docker.Controller.UnitRemoveContainerAsync();
            });

        ClearAllLogsCommand =
            new DelegateCommand(_ =>
            {
                foreach (var u in vm.Docker.Units.Units)
                    u.Logs.ClearAll();
            });

        ClearSelectedLogsCommand =
            new DelegateCommand(p =>
            {
                SelectUnitIfProvided(vm, p);
                vm.Docker.Units.SelectedUnit?.Logs.ClearAll();
            });

        OpenUnitCommand =
            new DelegateCommand(async p =>
            {
                SelectUnitIfProvided(vm, p);
                await vm.Docker.Controller.RefreshSystemStatusAsync();
                vm.Docker.Controller.StartTailLogs();
            });

        ExecuteInContainerCommand =
            new DelegateCommand(async p =>
                await vm.Docker.Controller.ExecuteInContainerAsync(p as string));
    }

    private void OnMenuAction(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return;

        // ✅ erstmal sichtbar machen dass es wirklich auslöst
        _vm.Threads.Append(ThreadId.T1, $"[MENU] {id}");

        // Hier später echte Aktionen rein:
        // switch (id) { case "network:enroll": ... }
        Console.WriteLine("[MENU] " + id);
    }
}
