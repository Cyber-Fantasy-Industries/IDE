using Avalonia.Media;

namespace GatewayIDE.App.Views.Docker;

public sealed class ServiceUnitVm : ViewModelBase
{
    public UnitConfig Config { get; }
    public DockerLogs Logs { get; } = new();

    // ✅ Phase 2: Runtime ist die einzige Status-Wahrheit
    public UnitRuntime Runtime { get; } = new();

    public string DisplayName => Config.DisplayName;

    // ✅ UI-friendly Text (damit XAML leicht bleibt)
    public string StatusText => Runtime.Status switch
    {
        UnitStatus.Up         => "UP",
        UnitStatus.Down       => "DOWN",
        UnitStatus.Starting   => "STARTING",
        UnitStatus.Restarting => "RESTARTING",
        UnitStatus.Building   => "BUILDING",
        UnitStatus.Error      => "ERROR",
        _                     => "UNKNOWN"
    };

    public IBrush StatusBrush => Runtime.Status switch
    {
        UnitStatus.Up         => Brushes.LimeGreen,
        UnitStatus.Down       => Brushes.OrangeRed,
        UnitStatus.Starting   => Brushes.DeepSkyBlue,
        UnitStatus.Restarting => Brushes.Gold,
        UnitStatus.Building   => Brushes.DeepSkyBlue,
        UnitStatus.Error      => Brushes.Red,
        _                     => Brushes.Gray
    };

    public ServiceUnitVm(UnitConfig config)
    {
        Config = config;

        // wenn Runtime.Status geändert wird, sollen UI Props mitziehen
        Runtime.PropertyChanged += (_, __) =>
        {
            Raise(nameof(StatusText));
            Raise(nameof(StatusBrush));
        };
    }

    public bool HasDevMode => Config.HasDevMode;
    public string? ActiveComposeProfile => IsDev ? "dev" : null;

    public bool IsDev
    {
        get => Runtime.Mode == UnitMode.Dev;
        set
        {
            Runtime.Mode = value ? UnitMode.Dev : UnitMode.Prod;
            Raise(nameof(IsDev));
            Raise(nameof(ActiveServiceName));
            Raise(nameof(ActiveContainerName));
        }
    }

    public string ActiveServiceName =>
        IsDev && !string.IsNullOrWhiteSpace(Config.DevServiceName)
            ? Config.DevServiceName!
            : Config.ServiceName;

    public string ActiveContainerName =>
        IsDev && !string.IsNullOrWhiteSpace(Config.DevContainerName)
            ? Config.DevContainerName!
            : Config.ContainerName;

}
