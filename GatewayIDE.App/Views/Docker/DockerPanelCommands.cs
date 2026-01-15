using System;
using System.Threading.Tasks;
using System.Windows.Input;
using GatewayIDE.App.Commands;

namespace GatewayIDE.App.Views.Docker;

public sealed class DockerPanelCommands
{
    private readonly DockerUi _docker;

    public ICommand RefreshCommand { get; }
    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand RestartCommand { get; }
    public ICommand DownCommand { get; }
    public ICommand RebuildCommand { get; }
    public ICommand RemoveContainerCommand { get; }
    public ICommand ExecCommand { get; }
    public ICommand OpenUnitCommand { get; }
    public ICommand ClearAllLogsCommand { get; }
    public ICommand ClearSelectedLogsCommand { get; }

    public DockerPanelCommands(DockerUi docker)
    {
        _docker = docker;

        RefreshCommand = new AsyncCommand(async _ => await _docker.Controller.RefreshSystemStatusAsync());

        StartCommand = new AsyncCommand(async p =>
        {
            SelectUnitIfProvided(p);
            await _docker.Controller.UnitStartAsync();
        });

        StopCommand = new AsyncCommand(async p =>
        {
            SelectUnitIfProvided(p);
            await _docker.Controller.UnitStopAsync();
        });

        RestartCommand = new AsyncCommand(async p =>
        {
            SelectUnitIfProvided(p);
            await _docker.Controller.UnitRestartAsync();
        });

        DownCommand = new AsyncCommand(async p =>
        {
            SelectUnitIfProvided(p);
            await _docker.Controller.UnitDownAsync();
        });

        RebuildCommand = new AsyncCommand(async p =>
        {
            SelectUnitIfProvided(p);
            await _docker.Controller.UnitFullRebuildAsync();
        });

        RemoveContainerCommand = new AsyncCommand(async p =>
        {
            SelectUnitIfProvided(p);
            await _docker.Controller.UnitRemoveContainerAsync();
        });

        ExecCommand = new AsyncCommand(async p => await _docker.Controller.ExecuteInContainerAsync(p as string));

        OpenUnitCommand = new AsyncCommand(async p =>
        {
            SelectUnitIfProvided(p);
            await _docker.Controller.RefreshSystemStatusAsync();
            _docker.Controller.StartTailLogs();
        });

        ClearAllLogsCommand = new AsyncCommand(_ =>
        {
            foreach (var u in _docker.Units.Units)
                u.Logs.ClearAll();
        });

        ClearSelectedLogsCommand = new AsyncCommand(p =>
        {
            SelectUnitIfProvided(p);
            _docker.Units.SelectedUnit?.Logs.ClearAll();
        });
    }

    private void SelectUnitIfProvided(object? parameter)
    {
        if (parameter is null) return;
        if (parameter is ServiceUnitVm vm)
            _docker.Units.SelectedUnit = vm;
    }
}
