using Avalonia.Controls;

namespace GatewayIDE.App.Views.Docker;

public partial class DockerPanel : UserControl
{
    public DockerPanel()
    {
        InitializeComponent();

        // Avalonia-stabiler Hook (kein EventArgs-Typ nötig)
        this.AttachedToVisualTree += (_, __) =>
        {
            if (DataContext is null)
            {
                try
                {
                    DataContext = App.Services.GetService(typeof(DockerPanelState))
                                   ?? new DockerPanelState();
                }
                catch
                {
                    DataContext = new DockerPanelState();
                }
            }
        };
    }
}
