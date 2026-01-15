using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using GatewayIDE.App.Services.Docker;
using Avalonia.VisualTree;


namespace GatewayIDE.App.Views.Docker;

/* =========================================================
 * DockerUi (Facade)
 * ========================================================= */
public sealed class DockerUi
{
    public DockerUnitsCatalog Units { get; }
    public DockerState State { get; }
    public DockerController Controller { get; }


    // Parameterlos -> XAML friendly
    public DockerUi()
    {
        Units = new DockerUnitsCatalog();
        State = new DockerState();
        Controller = new DockerController(State, Units);


        // 👉 EIN Ort für Unit-Definitionen
        Units.SetUnits(new[]
        {
            new ServiceUnitVm(new UnitConfig
            {
                Id = "network",
                DisplayName = "NETWORK",
                ComposeFile = "docker-compose.yml",
                ProjectName = "gateway-network",
                EnvFile = "net.dev.env",
                ServiceName = "network",
                ContainerName = "network-container",
                DevServiceName = "network-dev",
                DevContainerName = "network-dev-container"
            })
        });
    }


}

/* =========================================================
 * DockerController
 * ========================================================= */
public sealed class DockerController : ObservableBase
{
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

    // XAML friendly ctor
    public DockerController(DockerState state, DockerUnitsCatalog units)
    {
        _state = state;
        _units = units;

        // ✅ Phase 2.1: SelectedUnit Wechsel sauber handeln
        _units.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(DockerUnitsCatalog.SelectedUnit))
                OnSelectedUnitChanged();
        };
    }


    private ServiceUnitVm SelectedUnit =>
        _units.SelectedUnit ?? throw new InvalidOperationException("No unit selected.");

    // „effektive“ Config abhängig vom Toggle
    private UnitConfig EffectiveConfig => new UnitConfig
    {
        Id = SelectedUnit.Config.Id,
        DisplayName = SelectedUnit.Config.DisplayName,
        ComposeFile = SelectedUnit.Config.ComposeFile,
        ProjectName = SelectedUnit.Config.ProjectName,

        ServiceName = SelectedUnit.ActiveServiceName,
        ContainerName = SelectedUnit.ActiveContainerName,
        EnvFile = SelectedUnit.Config.EnvFile,
        // wichtig: profile dev nur wenn IsDev
        ComposeProfile = SelectedUnit.IsDev ? "dev" : null,

        // optional: dev-Felder weiterreichen (nicht zwingend)
        DevServiceName = SelectedUnit.Config.DevServiceName,
        DevContainerName = SelectedUnit.Config.DevContainerName
    };

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
            try { await RefreshSystemStatusAsync(epoch).ConfigureAwait(false); }
            catch { /* avoid unobserved */ }
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
                DesktopStatus.Open => "Open",
                DesktopStatus.Closed => "Closed",
                DesktopStatus.NotInstalled => "Not Installed",
                _ => "Unknown"
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

            if (epoch != _selectionEpoch) return;

            var st = await DockerService.GetUnitStatusAsync(EffectiveConfig).ConfigureAwait(false);

            if (epoch != _selectionEpoch) return;

            SetUnitStatus(MapContainerStatus(st));
        }
        catch (Exception ex)
        {
            if (epoch != _selectionEpoch) return;

            _state.DockerDesktopStatus = "Unknown";
            _state.DockerImageStatus = "Unknown";
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

        await DockerService.UnitUpAsync(EffectiveConfig, SelectedLogs.AppendDockerOut, SelectedLogs.AppendDockerErr)
            .ConfigureAwait(false);

        StartTailLogs();
        await RefreshSystemStatusAsync().ConfigureAwait(false);
    }

    public async Task UnitStopAsync()
    {
        SetUnitStatus(UnitStatus.Down);

        StopTailLogs();
        await DockerService.UnitStopAsync(EffectiveConfig, SelectedLogs.AppendDockerOut, SelectedLogs.AppendDockerErr)
            .ConfigureAwait(false);

        await RefreshSystemStatusAsync().ConfigureAwait(false);
    }

    public async Task UnitDownAsync()
    {
        StopTailLogs();
        await DockerService.UnitDownAsync(EffectiveConfig, SelectedLogs.AppendDockerOut, SelectedLogs.AppendDockerErr)
            .ConfigureAwait(false);

        await RefreshSystemStatusAsync().ConfigureAwait(false);
    }

    public async Task UnitRestartAsync()
    {
        SetUnitStatus(UnitStatus.Restarting);

        await DockerService.UnitRestartAsync(EffectiveConfig, SelectedLogs.AppendDockerOut, SelectedLogs.AppendDockerErr)
            .ConfigureAwait(false);

        StartTailLogs();
        await RefreshSystemStatusAsync().ConfigureAwait(false);
    }

    public async Task UnitRemoveContainerAsync()
    {
        try
        {
            SelectedLogs.AppendDockerOut($"[REMOVE] Entferne Container für {EffectiveConfig.DisplayName} …{Environment.NewLine}");

            await DockerService.UnitRemoveContainerAsync(EffectiveConfig, SelectedLogs.AppendDockerOut, SelectedLogs.AppendDockerErr)
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

            await DockerService.UnitFullRebuildAsync(EffectiveConfig, SelectedLogs.AppendDockerOut, SelectedLogs.AppendDockerErr)
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
            EffectiveConfig,
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
            EffectiveConfig,
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
        ContainerStatus.Running => UnitStatus.Up,
        ContainerStatus.Exited => UnitStatus.Down,
        ContainerStatus.NotFound => UnitStatus.Down,
        _ => UnitStatus.Unknown
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

/* =========================================================
 * DockerLogs
 * ========================================================= */
public sealed class DockerLogs : ObservableBase
{
    private readonly StringBuilder _dockerOut = new();
    private readonly StringBuilder _dockerErr = new();
    private readonly StringBuilder _gatewayLog = new();
    private readonly StringBuilder _containerIO = new();
    private readonly object _gate = new();

    public string GatewayLogBuffer => _gatewayLog.ToString();
    public string DockerOutBuffer => _dockerOut.ToString();
    public string DockerErrBuffer => _dockerErr.ToString();
    public string ContainerIOBuffer => _containerIO.ToString();

    public int GatewayLogCaret => _gatewayLog.Length;
    public int DockerOutCaret => _dockerOut.Length;
    public int DockerErrCaret => _dockerErr.Length;
    public int ContainerIOCaret => _containerIO.Length;

    public void AppendGateway(string s)
    {
        lock (_gate) { _gatewayLog.Append(s); }
        Raise(nameof(GatewayLogBuffer));
        Raise(nameof(GatewayLogCaret));
    }

    public void AppendDockerOut(string s)
    {
        lock (_gate) { _dockerOut.Append(s); }
        Raise(nameof(DockerOutBuffer));
        Raise(nameof(DockerOutCaret));
    }

    public void AppendDockerErr(string s)
    {
        lock (_gate) { _dockerErr.Append(s); }
        Raise(nameof(DockerErrBuffer));
        Raise(nameof(DockerErrCaret));
    }

    public void AppendContainerIO(string s)
    {
        lock (_gate) { _containerIO.Append(s); }
        Raise(nameof(ContainerIOBuffer));
        Raise(nameof(ContainerIOCaret));
    }

    public void ClearAll()
    {
        lock (_gate)
        {
            _dockerOut.Clear();
            _dockerErr.Clear();
            _gatewayLog.Clear();
            _containerIO.Clear();
        }

        Raise(nameof(DockerOutBuffer)); Raise(nameof(DockerOutCaret));
        Raise(nameof(DockerErrBuffer)); Raise(nameof(DockerErrCaret));
        Raise(nameof(GatewayLogBuffer)); Raise(nameof(GatewayLogCaret));
        Raise(nameof(ContainerIOBuffer)); Raise(nameof(ContainerIOCaret));
    }
}

/* =========================================================
 * DockerState
 * ========================================================= */
public sealed class DockerState : ObservableBase
{
    private string _dockerImageStatus = "None";
    public string DockerImageStatus
    {
        get => _dockerImageStatus;
        set { _dockerImageStatus = value; Raise(); Raise(nameof(DockerImageStatusBrush)); }
    }

    private string _dockerDesktopStatus = "Unknown";
    public string DockerDesktopStatus
    {
        get => _dockerDesktopStatus;
        set { _dockerDesktopStatus = value; Raise(); Raise(nameof(DockerDesktopStatusBrush)); }
    }

    public IBrush DockerImageStatusBrush =>
        DockerImageStatus == "Available" ? Brushes.LimeGreen :
        DockerImageStatus == "None" ? Brushes.Red :
        Brushes.Gray;

    public IBrush DockerDesktopStatusBrush =>
        DockerDesktopStatus == "Open" ? Brushes.LimeGreen :
        DockerDesktopStatus == "Closed" ? Brushes.Red :
        DockerDesktopStatus == "Not Installed" ? Brushes.Gray :
        Brushes.Gray;
}

/* =========================================================
 * DockerUnitsCatalog
 * ========================================================= */
public sealed class DockerUnitsCatalog : ObservableBase
{
    public ObservableCollection<ServiceUnitVm> Units { get; } = new();

    private ServiceUnitVm? _selectedUnit;
    public ServiceUnitVm? SelectedUnit
    {
        get => _selectedUnit;
        set { _selectedUnit = value; Raise(); }
    }

    public void SetUnits(IEnumerable<ServiceUnitVm> units)
    {
        Units.Clear();
        foreach (var u in units)
            Units.Add(u);

        SelectedUnit = Units.FirstOrDefault();
    }
}
