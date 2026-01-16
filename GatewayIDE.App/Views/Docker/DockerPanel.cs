using Avalonia.Controls;

namespace GatewayIDE.App.Views.Docker;

public partial class DockerPanel : UserControl
{
    public DockerPanel()
    {
        InitializeComponent();
        DataContext = new DockerPanelState();
    }
}