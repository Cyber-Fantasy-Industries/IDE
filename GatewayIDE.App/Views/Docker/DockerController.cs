using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GatewayIDE.App.Services.Processes;

namespace GatewayIDE.App.ViewModels;

public sealed class DockerController : ViewModelBase
{
    private readonly MainLayoutState _layout;
    private readonly DockerState _state;
    private readonly DockerUnitsCatalog _units;

    private CancellationTokenSource? _tailCts;

    // schützt gegen Race: alter Refresh darf neuen SelectedUnit-Status nicht überschreiben
    private int _selectionEpoch;

    private string _containerCommand = string.Empty;
    public string ContainerCommand
    {
        get => _containerCommand;
        set { _containerCommand = value; Raise(); }
    }

    public DockerController(MainLayoutState layout, DockerState state, DockerUnitsCatalog units)
    {
        _layout = layout;
        _state  = state;
        _units  = units;

        // ✅ Phase 2.1: SelectedUnit Wechsel sauber handeln
        _units.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(DockerUnitsCatalog.SelectedUnit))
                OnSelectedUnitChanged();
        };
    }

    private ServiceUnitVm SelectedUnit =>
        _units.SelectedUnit ?? throw new InvalidOperationException("No unit selected.");

    private UnitConfig SelectedConfig => SelectedUnit.Config;
    private DockerLogs SelectedLogs => SelectedUnit.Logs;
    private void OnSelectedUnitChanged()
    {
        // Tail immer stoppen, sonst schreibt der alte Stream weiter in ein neues Panel
        StopTailLogs();

        // Inputbox ist immer "per selected unit" gedacht -> reset hilft UX
        ContainerCommand = string.Empty;

        // Epoch hochzählen und Refresh fire&forget, aber race-sicher
        var epoch = Interlocked.Increment(ref _selectionEpoch);

        _ = Task.Run(async () =>
        {
            try
            {
                await RefreshSystemStatusAsync(epoch).ConfigureAwait(false);
            }
            catch
            {
                // RefreshSystemStatusAsync fängt intern schon ab,
                // hier vermeiden wir nur unobserved task exceptions.
            }
        });
    }

    // -----------------------------
    // Global Status (Desktop/Image) + SelectedUnit Status
    // -----------------------------
    public Task RefreshSystemStatusAsync()
        => RefreshSystemStatusAsync(_selectionEpoch);

    private async Task RefreshSystemStatusAsync(int epoch)
    {
        try
        {
            var desktop = await DockerService.GetDockerDesktopStatusAsync().ConfigureAwait(false);

            // Falls währenddessen Unit gewechselt wurde: abbrechen
            if (epoch != _selectionEpoch) return;

            _state.DockerDesktopStatus = desktop switch
            {
                DesktopStatus.Open         => "Open",
                DesktopStatus.Closed       => "Closed",
                DesktopStatus.NotInstalled => "Not Installed",
                _                          => "Unknown"
            };

            if (desktop != DesktopStatus.Open)
            {
                _state.DockerImageStatus = "None";
                SetUnitStatus(UnitStatus.Unknown);
                return;
            }

            _state.DockerImageStatus = await DockerService.IsImageAvailableAsync().ConfigureAwait(false)
                ? "Available"
                : "None";

            // Falls währenddessen Unit gewechselt wurde: abbrechen
            if (epoch != _selectionEpoch) return;

            var st = await DockerService.GetUnitStatusAsync(SelectedConfig).ConfigureAwait(false);

            if (epoch != _selectionEpoch) return;

            SetUnitStatus(MapContainerStatus(st));
        }
        catch (Exception ex)
        {
            if (epoch != _selectionEpoch) return;

            _state.DockerDesktopStatus = "Unknown";
            _state.DockerImageStatus   = "Unknown";
            SelectedUnit.Runtime.LastError = ex.Message;
            SetUnitStatus(UnitStatus.Unknown);
        }
    }

    // -----------------------------
    // Unit Actions (SelectedUnit)
    // -----------------------------

    public async Task UnitStartAsync()
    {
        SetUnitStatus(UnitStatus.Starting);

        await DockerService.UnitUpAsync(SelectedConfig, SelectedLogs.AppendDockerOut, SelectedLogs.AppendDockerErr)
            .ConfigureAwait(false);

        StartTailLogs();
        await RefreshSystemStatusAsync().ConfigureAwait(false);
    }

    public async Task UnitStopAsync()
    {
        SetUnitStatus(UnitStatus.Down);

        StopTailLogs();
        await DockerService.UnitStopAsync(SelectedConfig, SelectedLogs.AppendDockerOut, SelectedLogs.AppendDockerErr)
            .ConfigureAwait(false);

        await RefreshSystemStatusAsync().ConfigureAwait(false);
    }

    public async Task UnitDownAsync()
    {
        StopTailLogs();
        await DockerService.UnitDownAsync(SelectedConfig, SelectedLogs.AppendDockerOut, SelectedLogs.AppendDockerErr)
            .ConfigureAwait(false);

        await RefreshSystemStatusAsync().ConfigureAwait(false);
    }

    public async Task UnitRestartAsync()
    {
        SetUnitStatus(UnitStatus.Restarting);

        await DockerService.UnitRestartAsync(SelectedConfig, SelectedLogs.AppendDockerOut, SelectedLogs.AppendDockerErr)
            .ConfigureAwait(false);

        StartTailLogs();
        await RefreshSystemStatusAsync().ConfigureAwait(false);
    }

    public async Task UnitRemoveContainerAsync()
    {
        try
        {
            SelectedLogs.AppendDockerOut($"[REMOVE] Entferne Container für {SelectedConfig.DisplayName} …{Environment.NewLine}");

            await DockerService.UnitRemoveContainerAsync(SelectedConfig, SelectedLogs.AppendDockerOut, SelectedLogs.AppendDockerErr)
                .ConfigureAwait(false);

            await RefreshSystemStatusAsync().ConfigureAwait(false);
            SelectedLogs.AppendDockerOut("[REMOVE] Fertig." + Environment.NewLine);
        }
        catch (Exception ex)
        {
            SetUnitStatus(UnitStatus.Error);
            SelectedUnit.Runtime.LastError = ex.Message;
            SelectedLogs.AppendDockerErr("❌ Remove fehlgeschlagen: " + ex.Message + Environment.NewLine);
        }
    }

    public async Task UnitFullRebuildAsync()
    {
        try
        {
            SetUnitStatus(UnitStatus.Building);
            SelectedUnit.Runtime.LastError = null;
            SelectedLogs.ClearAll();

            await DockerService.UnitFullRebuildAsync(SelectedConfig, SelectedLogs.AppendDockerOut, SelectedLogs.AppendDockerErr)
                .ConfigureAwait(false);

            var banner =
                "################################" + Environment.NewLine +
                "######## Build Complete ########" + Environment.NewLine +
                "# You can start the Server now #" + Environment.NewLine +
                "################################" + Environment.NewLine;

            SelectedLogs.AppendDockerOut(banner);

            await RefreshSystemStatusAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            SetUnitStatus(UnitStatus.Error);
            SelectedUnit.Runtime.LastError = ex.Message;
            SelectedLogs.AppendDockerErr("❌ " + ex.Message + Environment.NewLine);
        }
    }

    public async Task ExecuteInContainerAsync(string? paramText = null)
    {
        var raw = paramText ?? ContainerCommand ?? string.Empty;
        var cmd = NormalizeShellCommand(raw);
        if (string.IsNullOrEmpty(cmd)) return;

        SelectedLogs.AppendContainerIO($"> {cmd}{Environment.NewLine}");

        await DockerService.UnitExecAsync(
            SelectedConfig,
            cmd,
            o => SelectedLogs.AppendContainerIO(o),
            e => SelectedLogs.AppendContainerIO(e)
        ).ConfigureAwait(false);

        ContainerCommand = string.Empty;
    }

    // -----------------------------
    // Logs Tail (SelectedUnit)
    // -----------------------------
    public void StartTailLogs()
    {
        StopTailLogs();
        _tailCts = new CancellationTokenSource();

        _ = DockerService.UnitTailLogsAsync(
            SelectedConfig,
            SelectedLogs.AppendGateway,
            SelectedLogs.AppendGateway,
            _tailCts.Token
        );
    }

    public void StopTailLogs()
    {
        _tailCts?.Cancel();
        _tailCts = null;
    }

    // -----------------------------
    // Helpers
    // -----------------------------
    private static UnitStatus MapContainerStatus(ContainerStatus st) => st switch
    {
        ContainerStatus.Running  => UnitStatus.Up,
        ContainerStatus.Exited   => UnitStatus.Down,
        ContainerStatus.NotFound => UnitStatus.Down,
        _                        => UnitStatus.Unknown
    };

    private static string NormalizeShellCommand(string input)
    {
        var lines = input
            .Replace("\r\n", "\n")
            .Split('\n')
            .Select(l => l.TrimEnd())
            .ToList();

        var result = new List<string>();

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (line.EndsWith("\\") || line.EndsWith("^"))
                result.Add(line[..^1].TrimEnd());
            else
            {
                result.Add(line);
                result.Add(" "); // separator
            }
        }

        return string.Join(" ", result).Trim();
    }

    private void SetUnitStatus(UnitStatus status)
    {
        SelectedUnit.Runtime.Status = status;
    }
}
