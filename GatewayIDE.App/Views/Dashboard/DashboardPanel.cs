using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GatewayIDE.App.Views.Dashboard;

public partial class DashboardPanel : UserControl
{
    public DashboardPanel()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
