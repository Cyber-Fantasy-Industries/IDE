using Avalonia.Media;

namespace GatewayIDE.App.ViewModels;

public sealed class DockerState : ViewModelBase
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
        DockerImageStatus == "None"      ? Brushes.Red :
                                          Brushes.Gray;

    public IBrush DockerDesktopStatusBrush =>
        DockerDesktopStatus == "Open"          ? Brushes.LimeGreen :
        DockerDesktopStatus == "Closed"        ? Brushes.Red :
        DockerDesktopStatus == "Not Installed" ? Brushes.Gray :
                                                Brushes.Gray;
}
