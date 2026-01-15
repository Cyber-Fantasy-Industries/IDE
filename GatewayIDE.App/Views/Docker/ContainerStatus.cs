namespace GatewayIDE.App.Views.Docker;

// Minimal-Enum, damit DockerPanelState compilebar ist.
// Erweitere Werte später nach Bedarf.
public enum ContainerStatus
{
    Unknown = 0,
    NotInstalled = 1,
    Stopped = 2,
    Running = 3,
    Error = 4
}