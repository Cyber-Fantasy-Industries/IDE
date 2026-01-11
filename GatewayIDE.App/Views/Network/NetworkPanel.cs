using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Microsoft.Extensions.DependencyInjection;

namespace GatewayIDE.App.Views.Network;
public partial class NetworkPanel : UserControl
{
    public NetworkPanel()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
