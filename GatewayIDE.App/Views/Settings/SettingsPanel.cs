using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GatewayIDE.App.Views.Settings;

public partial class SettingsPanel : UserControl
{
    public SettingsPanel()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
